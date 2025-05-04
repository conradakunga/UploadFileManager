using System.Text.Json;
using Azure.Identity;
using Azure.Storage.Blobs;

namespace Rad.UploadFileManager;

public class AzureBlobStorageEngine : IStorageEngine
{
    private readonly BlobContainerClient _dataContainerClient;
    private readonly BlobContainerClient _metadataContainerClient;

    /// <inheritdoc />
    public int TimeoutInMinutes { get; }

    public AzureBlobStorageEngine(int timeoutInMinutes, string account,
        string dataContainerName, string metadataContainerName)
    {
        TimeoutInMinutes = timeoutInMinutes;

        // Create a service client
        var blobServiceClient = new BlobServiceClient(
            new Uri($"https://{account}.blob.core.windows.net"),
            new DefaultAzureCredential());

        // Create container clients
        _dataContainerClient = blobServiceClient.CreateBlobContainer(dataContainerName);
        _metadataContainerClient = blobServiceClient.CreateBlobContainer(metadataContainerName);
        // Ensure they exist
        _dataContainerClient.CreateIfNotExists();
        _metadataContainerClient.CreateIfNotExists();
    }


    /// <inheritdoc />
    public async Task<FileMetadata> StoreFileAsync(FileMetadata metaData, Stream data,
        CancellationToken cancellationToken = default)
    {
        // Get the clients
        var dataClient = _dataContainerClient.GetBlobClient(metaData.FileId.ToString());
        var metadataClient = _metadataContainerClient.GetBlobClient(metaData.FileId.ToString());

        // Upload data in parallel
        await Task.WhenAll(
            metadataClient.UploadAsync(JsonSerializer.Serialize(metaData), cancellationToken),
            dataClient.UploadAsync(data, cancellationToken));

        return metaData;
    }

    /// <inheritdoc />
    public async Task<FileMetadata> GetMetadataAsync(Guid fileId, CancellationToken cancellationToken = default)
    {
        // Get the client
        var metadataClient = _metadataContainerClient.GetBlobClient(fileId.ToString());

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
        // Get the client
        var dataClient = _dataContainerClient.GetBlobClient(fileId.ToString());

        // Download the blob as a stream
        var response = await dataClient.DownloadStreamingAsync(cancellationToken: cancellationToken);

        // Download into a memory stream
        await using (var stream = response.Value.Content)
        {
            var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream, cancellationToken);
            return memoryStream;
        }
    }

    /// <inheritdoc />
    public async Task DeleteFileAsync(Guid fileId, CancellationToken cancellationToken = default)
    {
        // Get the clients
        var dataClient = _dataContainerClient.GetBlobClient(fileId.ToString());
        var metadataClient = _metadataContainerClient.GetBlobClient(fileId.ToString());

        // Delete in parallel
        await Task.WhenAll(
            metadataClient.DeleteAsync(cancellationToken: cancellationToken),
            dataClient.DeleteAsync(cancellationToken: cancellationToken));
    }

    /// <inheritdoc />
    public async Task<bool> FileExistsAsync(Guid fileId, CancellationToken cancellationToken = default)
    {
        // Get the client
        var dataClient = _dataContainerClient.GetBlobClient(fileId.ToString());
        // Check for existence
        return await dataClient.ExistsAsync(cancellationToken);
    }
}