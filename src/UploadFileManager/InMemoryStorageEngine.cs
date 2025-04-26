using System.Collections.Concurrent;

namespace Rad.UploadFileManager;

public class InMemoryStorageEngine : IStorageEngine
{
    // In-memory store for files and metadata
    private readonly ConcurrentDictionary<Guid, (FileMetadata MetaData, MemoryStream Stream)> _files;

    /// <summary>
    /// Constructor
    /// </summary>
    public InMemoryStorageEngine()
    {
        // Initialize the dictionary
        _files = new ConcurrentDictionary<Guid, (FileMetadata, MemoryStream)>();
    }

    /// <inheritdoc />
    public Task<FileMetadata> StoreFileAsync(FileMetadata metaData, Stream data,
        CancellationToken cancellationToken = default)
    {
        // Copy to a memory stream for storage
        var memoryStream = new MemoryStream();
        data.CopyTo(memoryStream);
        memoryStream.Position = 0;
        
        // Store the stream
        _files[metaData.FileId] = (metaData, memoryStream);
        return Task.FromResult(metaData);
    }

    /// <inheritdoc />
    public Task<FileMetadata> GetMetadataAsync(Guid fileId, CancellationToken cancellationToken = default)
    {
        // Try to fetch the file metadata 
        if (_files.TryGetValue(fileId, out var file))
        {
            return Task.FromResult(file.MetaData);
        }

        throw new FileNotFoundException();
    }

    /// <inheritdoc />
    public Task<Stream> GetFileAsync(Guid fileId, CancellationToken cancellationToken = default)
    {
        // Try to fetch the file
        if (_files.TryGetValue(fileId, out var file))
        {
            return Task.FromResult<Stream>(file.Stream);
        }

        throw new FileNotFoundException();
    }

    /// <inheritdoc />
    public Task DeleteFileAsync(Guid fileId, CancellationToken cancellationToken = default)
    {
        // Remove file. Whether the ID is there or not
        _files.Remove(fileId, out _);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> FileExistsAsync(Guid fileId, CancellationToken cancellationToken = default)
    {
        // Check if key is in the dictionary
        return Task.FromResult(_files.ContainsKey(fileId));
    }
}