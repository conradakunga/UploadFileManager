namespace Rad.UploadFileManager;

/// <summary>
/// File encryption & decryption contract
/// </summary>
public interface IFileEncryptor
{
    /// <summary>
    /// Compression algorithm to use
    /// </summary>
    EncryptionAlgorithm EncryptionAlgorithm { get; }

    /// <summary>
    /// Encrypt the stream
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public Stream Encrypt(Stream data);

    /// <summary>
    /// Decrypt the stream
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public Stream Decrypt(Stream data);
}