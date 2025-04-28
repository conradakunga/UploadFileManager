using System.Security.Cryptography;

namespace Rad.UploadFileManager;

public sealed class UploadFileManager : IUploadFileManager
{
    private readonly IFileCompressor _fileCompressor;
    private readonly IFileEncryptor _fileEncryptor;
    private readonly IStorageEngine _storageEngine;
    private readonly TimeProvider _timeProvider;

    public UploadFileManager(IStorageEngine storageEngine, IFileEncryptor fileEncryptor, IFileCompressor fileCompressor,
        TimeProvider timeProvider)
    {
        // Check that the injected services are valid
        ArgumentNullException.ThrowIfNull(storageEngine);
        ArgumentNullException.ThrowIfNull(fileEncryptor);
        ArgumentNullException.ThrowIfNull(fileCompressor);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _storageEngine = storageEngine;
        _fileEncryptor = fileEncryptor;
        _fileCompressor = fileCompressor;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Stores the file
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="extension"></param>
    /// <param name="data"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task<FileMetadata> UploadFileAsync(string fileName, string extension, Stream data,
        CancellationToken cancellationToken = default)
    {
        //Verify the passed in parameters are not null
        ArgumentNullException.ThrowIfNull(data);

        // Verify the fileName has valid characters
        FileNameValidator.Validate(fileName);

        // Verify the extension has valid characters
        ExtensionValidator.Validate(extension);

        //
        // Now carry out the work
        //

        // Compress the data
        var compressed = _fileCompressor.Compress(data);

        // Encrypt the data
        var encrypted = _fileEncryptor.Encrypt(compressed);

        // Build the metadata
        var fileID = Guid.CreateVersion7();
        var currentTime = _timeProvider.GetLocalNow().DateTime;
        byte[] hash;

        // Get a SHA256 hash of the original contents
        using (var sha = SHA256.Create())
            hash = await sha.ComputeHashAsync(data, cancellationToken);

        // Construct the metadata object
        var metadata = new FileMetadata
        {
            FileId = fileID,
            Name = fileName,
            Extension = extension,
            DateUploaded = currentTime,
            OriginalSize = data.Length,
            PersistedSize = encrypted.Length,
            CompressionAlgorithm = _fileCompressor.CompressionAlgorithm,
            EncryptionAlgorithm = _fileEncryptor.EncryptionAlgorithm,
            Hash = hash
        };

        // Persist the file data
        await _storageEngine.StoreFileAsync(metadata, encrypted, cancellationToken);

        // Reset the position of the data stream
        data.Position = 0;
        return metadata;
    }

    /// <summary>
    /// Get the file metadata
    /// </summary>
    /// <param name="fileId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<FileMetadata> FetchMetadataAsync(Guid fileId, CancellationToken cancellationToken = default)
    {
        // Verify that the file exists first
        if (await _storageEngine.FileExistsAsync(fileId, cancellationToken))
            return await _storageEngine.GetMetadataAsync(fileId, cancellationToken);

        throw new FileNotFoundException($"The file '{fileId}' was not found");
    }


    /// <summary>
    /// Get the file by ID
    /// </summary>
    /// <param name="fileId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<Stream> DownloadFileAsync(Guid fileId, CancellationToken cancellationToken = default)
    {
        // Verify that the file exists first
        if (await _storageEngine.FileExistsAsync(fileId, cancellationToken))
        {
            // Get the persisted file contents
            var persistedData = await _storageEngine.GetFileAsync(fileId, cancellationToken);
            // Decrypt the data
            var decryptedData = _fileEncryptor.Decrypt(persistedData);
            // Decompress the decrypted ata
            var uncompressedData = _fileCompressor.Decompress(decryptedData);
            return uncompressedData;
        }

        throw new FileNotFoundException($"The file '{fileId}' was not found");
    }

    /// <summary>
    /// Delete the file by ID
    /// </summary>
    /// <param name="fileId"></param>
    /// <param name="cancellationToken"></param>
    public async Task DeleteFileAsync(Guid fileId, CancellationToken cancellationToken = default)
    {
        // Verify that the file exists first
        if (await _storageEngine.FileExistsAsync(fileId, cancellationToken))
            await _storageEngine.DeleteFileAsync(fileId, cancellationToken);
        else
            throw new FileNotFoundException($"The file '{fileId}' was not found");
    }


    /// <summary>
    /// Check if the file exists by ID
    /// </summary>
    /// <param name="fileId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<bool> FileExistsAsync(Guid fileId, CancellationToken cancellationToken = default)
    {
        return await _storageEngine.FileExistsAsync(fileId, cancellationToken);
    }
}