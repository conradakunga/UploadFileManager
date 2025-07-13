using System.Security.Cryptography;
using System.Text;
using Bogus;
using AwesomeAssertions;
using Rad.UploadFileManager;

namespace UploadFileManagerTests;

[Trait("Type", "Unit")]
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

    [Theory]
    [InlineData(256)]
    [InlineData(512)]
    [InlineData(1_024)]
    public async Task Large_File_Encryption_And_Decryption_Succeeds(int size)
    {
        var fileSizeInBytes = 1L * size * 1_024 * 1_024;
        const int bufferSize = 1024 * 1024;

        byte[] buffer = new byte[bufferSize];
        new Random().NextBytes(buffer);

        var filePath = Path.GetTempFileName();
        await using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            long bytesWritten = 0;
            while (bytesWritten < fileSizeInBytes)
            {
                long bytesToWrite = Math.Min(bufferSize, fileSizeInBytes - bytesWritten);
                await fs.WriteAsync(buffer.AsMemory(0, (int)bytesToWrite));
                bytesWritten += bytesToWrite;
            }
        }

        using (var sha = SHA256.Create())
        {
            await using (var input = File.OpenRead(filePath))
            {
                // Compute the hash of the uncompressed data
                var originalHash = await sha.ComputeHashAsync(input);
                // Reset the input position!
                input.Position = 0;
                var encryptor = new AesFileEncryptor(_key, _iv);
                // Encrypt the stream and fetch data
                await using (var encryptedStream = encryptor.Encrypt(input))
                {
                    await using (var decryptedStream = encryptor.Decrypt(encryptedStream))
                    {
                        // Compute the decrypted stream hash
                        var currentHash = await sha.ComputeHashAsync(decryptedStream);
                        // Verify success
                        currentHash.Should().BeEquivalentTo(originalHash);
                    }
                }
            }
        }

        File.Delete(filePath);
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