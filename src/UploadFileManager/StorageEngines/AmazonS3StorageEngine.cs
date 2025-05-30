using System.Net;
using System.Text;
using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Amazon.S3.Util;

namespace Rad.UploadFileManager.StorageEngines;

public sealed class AmazonS3StorageEngine : IStorageEngine
{
    private readonly string _dataContainerName;
    private readonly string _metadataContainerName;
    private readonly TransferUtility _utility;
    private readonly AmazonS3Client _client;

    private AmazonS3StorageEngine(string username, string password, string amazonLocation, string dataContainerName,
        string metadataContainerName)
    {
        // Configuration for the amazon s3 client
        var config = new AmazonS3Config
        {
            ServiceURL = amazonLocation,
            ForcePathStyle = true
        };

        _dataContainerName = dataContainerName;
        _metadataContainerName = metadataContainerName;
        _client = new AmazonS3Client(username, password, config);
        _utility = new TransferUtility(_client);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="username"></param>
    /// <param name="password"></param>
    /// <param name="amazonLocation"></param>
    /// <param name="dataContainerName"></param>
    /// <param name="metadataContainerName"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task<AmazonS3StorageEngine> InitializeAsync(string username, string password,
        string amazonLocation,
        string dataContainerName,
        string metadataContainerName, CancellationToken cancellationToken = default)
    {
        var engine = new AmazonS3StorageEngine(username, password, amazonLocation, dataContainerName,
            metadataContainerName);

        // Configuration for the amazon s3 client
        var config = new AmazonS3Config
        {
            ServiceURL = amazonLocation,
            ForcePathStyle = true
        };

        var client = new AmazonS3Client(username, password, config);
        // Check if the metadata bucket exists
        if (!await AmazonS3Util.DoesS3BucketExistV2Async(client, metadataContainerName))
        {
            var request = new PutBucketRequest
            {
                BucketName = metadataContainerName,
                UseClientRegion = true
            };

            await client.PutBucketAsync(request, cancellationToken);
        }

        // Check if the data bucket exists
        if (!await AmazonS3Util.DoesS3BucketExistV2Async(client, dataContainerName))
        {
            var request = new PutBucketRequest
            {
                BucketName = dataContainerName,
                UseClientRegion = true
            };

            await client.PutBucketAsync(request, cancellationToken);
        }

        return engine;
    }

    public int TimeoutInMinutes => 0;

    /// <inheritdoc />
    public async Task<FileMetadata> StoreFileAsync(FileMetadata metaData, Stream data,
        CancellationToken cancellationToken = default)
    {
        // Upload the data and the metadata in parallel
        await Task.WhenAll(
            _utility.UploadAsync(new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(metaData))),
                _metadataContainerName, metaData.FileId.ToString(), cancellationToken),
            _utility.UploadAsync(data, _dataContainerName, metaData.FileId.ToString(), cancellationToken)
        );
        return metaData;
    }

    /// <inheritdoc />
    public async Task<FileMetadata> GetMetadataAsync(Guid fileId, CancellationToken cancellationToken = default)
    {
        //Verify file exists
        if (!await FileExistsAsync(fileId, _metadataContainerName, cancellationToken))
            throw new FileNotFoundException($"File {fileId} not found");

        // Create a request
        var request = new GetObjectRequest
        {
            BucketName = _metadataContainerName,
            Key = fileId.ToString()
        };

        // Retrieve the data
        using var response = await _client.GetObjectAsync(request, cancellationToken);
        await using var responseStream = response.ResponseStream;
        var memoryStream = new MemoryStream();
        await responseStream.CopyToAsync(memoryStream, cancellationToken);

        // Reset position
        memoryStream.Position = 0;
        using var reader = new StreamReader(memoryStream);
        var content = await reader.ReadToEndAsync(cancellationToken);
        return JsonSerializer.Deserialize<FileMetadata>(content) ?? throw new FileNotFoundException();
    }

    /// <inheritdoc />
    public async Task<Stream> GetFileAsync(Guid fileId, CancellationToken cancellationToken = default)
    {
        //Verify file exists
        if (!await FileExistsAsync(fileId, _dataContainerName, cancellationToken))
            throw new FileNotFoundException($"File {fileId} not found");

        // Create a request
        var request = new GetObjectRequest
        {
            BucketName = _dataContainerName,
            Key = fileId.ToString()
        };

        // Retrieve the data
        using var response = await _client.GetObjectAsync(request, cancellationToken);
        await using var responseStream = response.ResponseStream;
        var memoryStream = new MemoryStream();
        await responseStream.CopyToAsync(memoryStream, cancellationToken);
        // Reset position
        memoryStream.Position = 0;
        return memoryStream;
    }

    /// <inheritdoc />
    public async Task DeleteFileAsync(Guid fileId, CancellationToken cancellationToken = default)
    {
        //Verify file exists
        if (!await FileExistsAsync(fileId, _dataContainerName, cancellationToken))
            throw new FileNotFoundException($"File {fileId} not found");

        // Delete metadata and data in parallel
        await Task.WhenAll(
            _client.DeleteObjectAsync(_metadataContainerName, fileId.ToString(), cancellationToken),
            _client.DeleteObjectAsync(_dataContainerName, fileId.ToString(), cancellationToken));
    }

    /// <inheritdoc />
    public async Task<bool> FileExistsAsync(Guid fileId, CancellationToken cancellationToken = default)
    {
        return await FileExistsAsync(fileId, _dataContainerName, cancellationToken);
    }

    private async Task<bool> FileExistsAsync(Guid fileId, string containerName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _client.GetObjectMetadataAsync(containerName, fileId.ToString(), cancellationToken);
            return true;
        }
        catch (AmazonS3Exception ex)
        {
            if (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return false;
            }

            throw;
        }
    }
}