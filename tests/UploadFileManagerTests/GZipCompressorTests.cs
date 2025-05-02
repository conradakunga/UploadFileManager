using System.Security.Cryptography;
using System.Text;
using Bogus;
using FluentAssertions;
using Rad.UploadFileManager;

namespace UploadFileManagerTests;

[Trait("Type", "Unit")]
public class GZipCompressorTests
{
    private readonly GZipCompressor _gzipCompressor;

    public GZipCompressorTests()
    {
        _gzipCompressor = new GZipCompressor();
    }

    [Fact]
    public void After_Compression_Stream_Should_Be_Smaller()
    {
        // Arrange

        var faker = new Faker();
        var originalData = faker.Lorem.Sentences(10);
        var streamToCompress = new MemoryStream(Encoding.UTF8.GetBytes(originalData));
        var originalStreamSize = streamToCompress.Length;

        // Act

        var compressedStream = _gzipCompressor.Compress(streamToCompress);
        var compressedStreamSize = compressedStream.Length;
        var decompressedStream = _gzipCompressor.Decompress(compressedStream);

        var decompressedData = Encoding.UTF8.GetString(decompressedStream.GetBytes());

        // Assert

        // Check that the size is smaller, or at worst, equal
        compressedStreamSize.Should().BeLessThanOrEqualTo(originalStreamSize);
        // Check that the decompression was successful
        originalData.Should().Be(decompressedData);
        // Check the compression mode
        _gzipCompressor.CompressionAlgorithm.Should().Be(CompressionAlgorithm.Zip);
    }

    [Theory]
    [InlineData(256)]
    [InlineData(512)]
    [InlineData(1_024)]
    public async Task Large_File_Compression_Succeeds(int size)
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
                // Compress the stream
                await using (var compressed = _gzipCompressor.Compress(input))
                {
                    await using (var decompressed = _gzipCompressor.Decompress(compressed))
                    {
                        // Compute the hash for the compressed ata
                        var currentHash = await sha.ComputeHashAsync(decompressed);
                        // Compare original and current hashes
                        currentHash.Should().BeEquivalentTo(originalHash);
                    }
                }
            }
        }

        File.Delete(filePath);
    }

    [Fact]
    public void Gzip_Compression_Implementation_Is_Valid()
    {
        var faker = new Faker();
        var originalData = faker.Lorem.Sentences(10);
        var originalDataBytes = Encoding.UTF8.GetBytes(originalData);
        using (var streamToCompress = new MemoryStream(originalDataBytes))
        {
            // Create a memory stream to hold the compressed data
            using (var compressedStream = new MemoryStream())
            {
                // Use DotNetZip's GZipStream to compress
                using (var gzipStream = new Ionic.Zlib.GZipStream(compressedStream, Ionic.Zlib.CompressionMode.Compress,
                           leaveOpen: true))
                {
                    streamToCompress.CopyTo(gzipStream);
                }

                compressedStream.Position = 0;

                // Decompress using GZipStream
                using (var decompressedStream = new MemoryStream())
                {
                    using (var gzipStream =
                           new Ionic.Zlib.GZipStream(compressedStream, Ionic.Zlib.CompressionMode.Decompress))
                    {
                        gzipStream.CopyTo(decompressedStream);
                    }

                    // Reset the position of the decompressed stream to the beginning
                    decompressedStream.Position = 0;

                    var originalFromDotNetZip = decompressedStream.ToArray();

                    // Now compress and decompress using our component
                    var originalFromComponent =
                        _gzipCompressor.Decompress(_gzipCompressor.Compress(streamToCompress)).GetBytes();
                    originalFromDotNetZip.Should().BeEquivalentTo(originalFromComponent);
                }
            }
        }
    }
}