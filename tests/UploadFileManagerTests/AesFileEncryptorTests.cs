using System.Security.Cryptography;
using System.Text;
using Bogus;
using FluentAssertions;
using Rad.UploadFileManager;

namespace UploadFileManagerTests;

public class AesFileEncryptorTests
{
    private readonly byte[] _key;
    private readonly byte[] _iv;

    public AesFileEncryptorTests()
    {
        using var aes = Aes.Create();
        aes.KeySize = 256;
        aes.GenerateKey();
        aes.GenerateIV();

        _key = aes.Key;
        _iv = aes.IV;
    }

    [Fact]
    public void Encryption_And_Decryption_Is_Successful()
    {
        var faker = new Faker();
        var originalData = faker.Lorem.Sentences(10);
        var dataToCompress = Encoding.UTF8.GetBytes(originalData);
        var streamToCompress = new MemoryStream(dataToCompress);
        var encryptor = new AesFileEncryptor(_key, _iv);

        // Encrypt the stream and fetch data
        using (var encryptedStream = encryptor.Encrypt(streamToCompress))
        {
            var encryptedData = encryptedStream.GetBytes();

            using (var decryptedStream = encryptor.Decrypt(encryptedStream))
            {
                var decryptedData = decryptedStream.GetBytes();

                // Check encryption actually changed the data
                encryptedData.Should().NotBeEquivalentTo(originalData);
                // Check decompressed data matches original data
                decryptedData.Should().BeEquivalentTo(dataToCompress);
            }
        }
    }
}