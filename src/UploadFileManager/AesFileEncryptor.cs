using System.Security.Cryptography;
using System.Text;

namespace Rad.UploadFileManager;

/// <summary>
/// Component to use Aes to encrypt streams
/// </summary>
public sealed class AesFileEncryptor : IFileEncryptor
{
    /// <inheritdoc />
    public EncryptionAlgorithm EncryptionAlgorithm => EncryptionAlgorithm.Aes;

    private readonly byte[] _key;
    private readonly byte[] _iv;

    /// <summary>
    /// Constructor that takes AES parameters as string
    /// </summary>
    /// <param name="key"></param>
    /// <param name="iv"></param>
    public AesFileEncryptor(string key, string iv)
    {
        _key = Encoding.Default.GetBytes(key);
        _iv = Encoding.Default.GetBytes(iv);
    }

    /// <summary>
    /// Constructor that takes AES parameters as byte arrays 
    /// </summary>
    /// <param name="key"></param>
    /// <param name="iv"></param>
    public AesFileEncryptor(byte[] key, byte[] iv)
    {
        _key = key;
        _iv = iv;
    }

    /// <inheritdoc />
    public Stream Encrypt(Stream data)
    {
        // Create Aes object & initialize
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        var outputStream = new MemoryStream();

        // Encrypt
        using (var cryptoStream =
               new CryptoStream(outputStream, aes.CreateEncryptor(), CryptoStreamMode.Write, leaveOpen: true))
        {
            data.CopyTo(cryptoStream);
            cryptoStream.FlushFinalBlock();
        }

        outputStream.Position = 0;
        return outputStream;
    }

    /// <inheritdoc />
    public Stream Decrypt(Stream data)
    {
        // Create Aes object & initialize
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        var outputStream = new MemoryStream();

        // Decrypt
        using (var cryptoStream = new CryptoStream(data, aes.CreateDecryptor(), CryptoStreamMode.Read, leaveOpen: true))
        {
            cryptoStream.CopyTo(outputStream);
        }

        outputStream.Position = 0;
        return outputStream;
    }
}