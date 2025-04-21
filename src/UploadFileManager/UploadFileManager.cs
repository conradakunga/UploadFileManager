using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Rad.UploadFileManager;

public sealed class UploadFileManager : IUploadFileManager
{
    private readonly IFileCompressor _fileCompressor;
    private readonly IFileEncryptor _fileEncryptor;
    private readonly IFilePersistor _filePersistor;

    public UploadFileManager(IFilePersistor filePersistor, IFileEncryptor fileEncryptor, IFileCompressor fileCompressor)
    {
        // Check that the injected services are valid
        ArgumentNullException.ThrowIfNull(filePersistor);
        ArgumentNullException.ThrowIfNull(fileEncryptor);
        ArgumentNullException.ThrowIfNull(fileCompressor);

        _filePersistor = filePersistor;
        _fileEncryptor = fileEncryptor;
        _fileCompressor = fileCompressor;
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
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(extension);
        ArgumentNullException.ThrowIfNull(data);

        // Verify the fileName has valid characters
        var invalidCharacters = Path.GetInvalidFileNameChars();
        if (invalidCharacters.Any(fileName.Contains))
            throw new ArgumentException($"The file name '{fileName}' contains invalid characters");

        // Verify the extension has valid characters
        if (invalidCharacters.Any(extension.Contains))
            throw new ArgumentException($"The extension '{extension}' contains invalid characters");

        // Validate the regex for the extension
        if (!Regex.IsMatch(extension, @"^\.\w+$"))
            throw new ArgumentException($"The extension {extension}' does not conform to the expected format: .xxx");

        //
        // Now carry out the work
        //

        // Compress the data
        var compressed = _fileCompressor.Compress(data);
        // Encrypt the data
        var encrypted = _fileEncryptor.Encrypt(compressed);

        // Build the metadata
        var fileID = Guid.CreateVersion7();
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
            DateUploaded = DateTime.Now,
            OriginalSize = data.Length,
            PersistedSize = encrypted.Length,
            CompressionAlgorithm = _fileCompressor.CompressionAlgorithm,
            EncryptionAlgorithm = _fileEncryptor.EncryptionAlgorithm,
            Hash = hash
        };

        // Persist the file data
        await _filePersistor.StoreFileAsync(fileName, extension, encrypted, cancellationToken);
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
        if (await _filePersistor.FileExistsAsync(fileId, cancellationToken))
            return await _filePersistor.GetMetadataAsync(fileId, cancellationToken);

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
        if (await _filePersistor.FileExistsAsync(fileId, cancellationToken))
        {
            // Get the persisted file contents
            var persistedData = await _filePersistor.GetFileAsync(fileId, cancellationToken);
            // Decrypt the data
            var decryptedData = _fileEncryptor.Decrypt(persistedData);
            // Decompress the decrypted ata
            var uncompressedData = _fileCompressor.DeCompress(decryptedData);
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
        if (await _filePersistor.FileExistsAsync(fileId, cancellationToken))
            await _filePersistor.DeleteFileAsync(fileId, cancellationToken);

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
        return await _filePersistor.FileExistsAsync(fileId, cancellationToken);
    }
}