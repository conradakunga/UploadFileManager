namespace Rad.UploadFileManager;

public interface IFilePersistor
{
    Task<FileMetadata> StoreFileAsync(string fileName, string extension, Stream data,
        CancellationToken cancellationToken = default);

    Task<FileMetadata> GetMetadataAsync(Guid fileId, CancellationToken cancellationToken = default);

    Task<Stream> GetFileAsync(Guid fileId, CancellationToken cancellationToken = default);
    Task DeleteFileAsync(Guid fileId, CancellationToken cancellationToken = default);
    Task<bool> FileExistsAsync(Guid fileId, CancellationToken cancellationToken = default);
}