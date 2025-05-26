using System.Text;
using System.Text.Json;
using Azure.Storage;
using Azure.Storage.Blobs;

namespace Rad.UploadFileManager.StorageEngines;

public class AzureBlobStorageEngine : IStorageEngine
{
    private readonly string _accountName;
    private readonly string _accountKey;
    private readonly string _azureLocation;
    private readonly string _dataContainerName;
    private readonly string _metadataContainerName;
    private readonly BlobContainerClient _dataContainerClient;
    private readonly BlobContainerClient _metadataContainerClient;

    /// <inheritdoc />
    public int TimeoutInMinutes { get; }

    public AzureBlobStorageEngine(int timeoutInMinutes, string accountName, string accountKey, string azureLocation,
        string dataContainerName, string metadataContainerName)
    {
        _accountName = accountName;
        _accountKey = accountKey;
        _azureLocation = azureLocation;
        _dataContainerName = dataContainerName;
        _metadataContainerName = metadataContainerName;
        TimeoutInMinutes = timeoutInMinutes;

        // Create a service client
        var blobServiceClient = new BlobServiceClient(
            new Uri($"{azureLocation}/{accountName}/"),
            new StorageSharedKeyCredential(accountName, accountKey));

        // Get our container clients
        _dataContainerClient = blobServiceClient.GetBlobContainerClient(dataContainerName);
        _metadataContainerClient = blobServiceClient.GetBlobContainerClient(metadataContainerName);
    }

    /// <summary>
    /// Initialize the engine
    /// </summary>
    /// <param name="accountName"></param>
    /// <param name="accountKey"></param>
    /// <param name="azureLocation"></param>
    /// <param name="dataContainerName"></param>
    /// <param name="metadataContainerName"></param>
    /// <param name="cancellationToken"></param>
    public async Task InitializeAsync(string accountName, string accountKey, string azureLocation,
        string dataContainerName, string metadataContainerName, CancellationToken cancellationToken = default)
    {
        // Create a service client
        var blobServiceClient = new BlobServiceClient(
            new Uri($"{azureLocation}/{accountName}/"),
            new StorageSharedKeyCredential(accountName, accountKey));

        // Get our container clients
        var dataContainerClient = blobServiceClient.GetBlobContainerClient(dataContainerName);
        var metadataContainerClient = blobServiceClient.GetBlobContainerClient(metadataContainerName);

        // Ensure they exist
        if (!await dataContainerClient.ExistsAsync(cancellationToken))
            await dataContainerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        if (!await metadataContainerClient.ExistsAsync(cancellationToken))
            await metadataContainerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
    }


    /// <inheritdoc />
    public async Task<FileMetadata> StoreFileAsync(FileMetadata metaData, Stream data,
        CancellationToken cancellationToken = default)
    {
        // Initialize
        await InitializeAsync(_accountName, _accountKey, _azureLocation, _dataContainerName, _metadataContainerName,
            cancellationToken);

        // Get the clients
        var dataClient = _dataContainerClient.GetBlobClient(metaData.FileId.ToString());
        var metadataClient = _metadataContainerClient.GetBlobClient(metaData.FileId.ToString());

        // Upload data in parallel
        await Task.WhenAll(
            metadataClient.UploadAsync(new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(metaData))),
                cancellationToken),
            dataClient.UploadAsync(data, cancellationToken));

        data.Position = 0;

        return metaData;
    }

    /// <inheritdoc />
    public async Task<FileMetadata> GetMetadataAsync(Guid fileId, CancellationToken cancellationToken = default)
    {
        // Initialize
        await InitializeAsync(_accountName, _accountKey, _azureLocation, _dataContainerName, _metadataContainerName,
            cancellationToken);

        // Get the client
        var metadataClient = _metadataContainerClient.GetBlobClient(fileId.ToString());

        if (!await metadataClient.ExistsAsync(cancellationToken))
        {
            throw new FileNotFoundException($"File {fileId} not found");
        }

        // Retrieve the metadata
        var result = await metadataClient.DownloadContentAsync(cancellationToken: cancellationToken);
        if (result != null && result.HasValue)
        {
            return JsonSerializer.Deserialize<FileMetadata>(result.Value!.Content!.ToString())!;
        }

        throw new FileNotFoundException($"File {fileId} not found");
    }

    /// <inheritdoc />
    public async Task<Stream> GetFileAsync(Guid fileId, CancellationToken cancellationToken = default)
    {
        // Initialize
        await InitializeAsync(_accountName, _accountKey, _azureLocation, _dataContainerName, _metadataContainerName,
            cancellationToken);

        // Get the client
        var dataClient = _dataContainerClient.GetBlobClient(fileId.ToString());

        if (!await FileExistsAsync(fileId, cancellationToken))
        {
            throw new FileNotFoundException($"File {fileId} not found");
        }

        // Download the blob as a stream
        var response = await dataClient.DownloadStreamingAsync(cancellationToken: cancellationToken);

        // Download into a memory stream
        await using (var stream = response.Value.Content)
        {
            var memoryStream = new MemoryStream();
            // Copy to memory stream
            await stream.CopyToAsync(memoryStream, cancellationToken);
            // Reset position
            memoryStream.Position = 0;
            return memoryStream;
        }
    }

    /// <inheritdoc />
    public async Task DeleteFileAsync(Guid fileId, CancellationToken cancellationToken = default)
    {
        // Initialize
        await InitializeAsync(_accountName, _accountKey, _azureLocation, _dataContainerName, _metadataContainerName,
            cancellationToken);

        // Get the clients
        var dataClient = _dataContainerClient.GetBlobClient(fileId.ToString());
        var metadataClient = _metadataContainerClient.GetBlobClient(fileId.ToString());

        if (!await FileExistsAsync(fileId, cancellationToken))
        {
            throw new FileNotFoundException($"File {fileId} not found");
        }

        // Delete in parallel
        await Task.WhenAll(
            metadataClient.DeleteAsync(cancellationToken: cancellationToken),
            dataClient.DeleteAsync(cancellationToken: cancellationToken));
    }

    /// <inheritdoc />
    public async Task<bool> FileExistsAsync(Guid fileId, CancellationToken cancellationToken = default)
    {
        // Initialize
        await InitializeAsync(_accountName, _accountKey, _azureLocation, _dataContainerName, _metadataContainerName,
            cancellationToken);

        // Get the client
        var dataClient = _dataContainerClient.GetBlobClient(fileId.ToString());
        // Check for existence
        return await dataClient.ExistsAsync(cancellationToken);
    }
}