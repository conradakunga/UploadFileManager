namespace UploadFileManager;

public interface IFileManager
{
    Task<FileMetadata> UploadFileAsync(string fileName, string extension, Stream data,
        CancellationToken cancellationToken = default);

    Task<FileMetadata> FetchMetadataAsync(Guid fileId, CancellationToken cancellationToken = default);
    Task<Stream> DownloadFileAsync(Guid fileId, CancellationToken cancellationToken = default);
    Task<Stream> DeleteFileAsync(Guid fileId, CancellationToken cancellationToken = default);
    Task<bool> FileExistsAsync(Guid fileId, CancellationToken cancellationToken = default);
}