namespace Rad.UploadFileManager;

/// <summary>
/// Metadata of store files
/// </summary>
public sealed record FileMetadata
{
    /// <summary>
    /// File identifier
    /// </summary>
    public required Guid FileId { get; init; }

    /// <summary>
    /// Full file name
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// File extension
    /// </summary>
    public required string Extension { get; init; }

    /// <summary>
    ///  Date and time of storage
    /// </summary>
    public required DateTime DateUploaded { get; init; }

    /// <summary>
    /// Original file size
    /// </summary>
    public required long OriginalSize { get; init; }

    /// <summary>
    /// Compressed file size
    /// </summary>
    public required long PersistedSize { get; init; }

    /// <summary>
    /// Compression algorithm used to compress file
    /// </summary>
    public required CompressionAlgorithm CompressionAlgorithm { get; init; }

    /// <summary>
    /// Encryption algorithm used to encrypt file
    /// </summary>
    public required EncryptionAlgorithm EncryptionAlgorithm { get; init; }

    /// <summary>
    /// SHA256 hash of the file
    /// </summary>
    public required byte[] Hash { get; set; }
}