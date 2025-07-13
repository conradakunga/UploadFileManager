using System.Net;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Storage.v1.Data;
using Google.Cloud.Storage.V1;

namespace Rad.UploadFileManager.StorageEngines;

public sealed class GoogleCloudStorageEngine : IStorageEngine
{
    private readonly string _dataContainerName;
    private readonly string _metadataContainerName;
    private readonly StorageClient _client;

    private GoogleCloudStorageEngine(string accessToken, string dataContainerName,
        string metadataContainerName)
    {
        // Configuration for the Google client
        var credential = GoogleCredential.FromAccessToken(accessToken);

        _dataContainerName = dataContainerName;
        _metadataContainerName = metadataContainerName;
        _client = StorageClient.Create(credential);
    }

    /// <summary>
    /// Initialize the storage engine 
    /// </summary>
    /// <param name="accessToken"></param>
    /// <param name="settings"></param>
    /// <param name="dataContainerName"></param>
    /// <param name="metadataContainerName"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task<GoogleCloudStorageEngine> InitializeAsync(string accessToken, GoogleSettings settings,
        string dataContainerName,
        string metadataContainerName,
        CancellationToken cancellationToken = default)
    {
        var engine = new GoogleCloudStorageEngine(accessToken, dataContainerName,
            metadataContainerName);

        var client = await StorageClient.CreateAsync(GoogleCredential.FromAccessToken(accessToken));

        // Check if the metadata bucket exists
        try
        {
            await client.GetBucketAsync(metadataContainerName, cancellationToken: cancellationToken);
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            await client.CreateBucketAsync(settings.ProjectID, new Bucket
            {
                Name = metadataContainerName,
                Location = settings.BucketLocation,
                StorageClass = StorageClasses.Standard
            }, cancellationToken: cancellationToken);
        }

        // Check if the data bucket exists
        try
        {
            await client.GetBucketAsync(dataContainerName, cancellationToken: cancellationToken);
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            await client.CreateBucketAsync(settings.ProjectID, new Bucket
            {
                Name = dataContainerName,
                Location = settings.BucketLocation,
                StorageClass = StorageClasses.Standard
            }, cancellationToken: cancellationToken);
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
            _client.UploadObjectAsync(_metadataContainerName, objectName: metaData.FileId.ToString(),
                MediaTypeNames.Application.Json,
                source: new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(metaData))),
                cancellationToken: cancellationToken),
            _client.UploadObjectAsync(_dataContainerName, objectName: metaData.FileId.ToString(),
                null, source: data, cancellationToken: cancellationToken));
        return metaData;
    }

    /// <inheritdoc />
    public async Task<FileMetadata> GetMetadataAsync(Guid fileId, CancellationToken cancellationToken = default)
    {
        //Verify file exists
        if (!await FileExistsAsync(fileId, _metadataContainerName, cancellationToken))
            throw new FileNotFoundException($"File {fileId} not found");

        // Retrieve the data
        using var memoryStream = new MemoryStream();
        await _client.DownloadObjectAsync(_metadataContainerName, fileId.ToString(), memoryStream,
            cancellationToken: cancellationToken);
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

        var memoryStream = new MemoryStream();
        await _client.DownloadObjectAsync(_dataContainerName, fileId.ToString(), memoryStream,
            cancellationToken: cancellationToken);
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
            _client.DeleteObjectAsync(_metadataContainerName, fileId.ToString(), cancellationToken: cancellationToken),
            _client.DeleteObjectAsync(_dataContainerName, fileId.ToString(), cancellationToken: cancellationToken));
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
            await _client.GetObjectAsync(containerName, fileId.ToString(), cancellationToken: cancellationToken);
            return true;
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }
}