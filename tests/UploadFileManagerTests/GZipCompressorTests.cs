using System.IO.Compression;
using System.Text;
using Bogus;
using FluentAssertions;
using Rad.UploadFileManager;

namespace UploadFileManagerTests;

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
        var streamToCompress = new MemoryStream(Encoding.Default.GetBytes(originalData));
        var originalStreamSize = streamToCompress.Length;

        // Act

        var compressedStream = _gzipCompressor.Compress(streamToCompress);
        var compressedStreamSize = compressedStream.Length;
        var decompressedStream = _gzipCompressor.Decompress(compressedStream);

        var decompressedData = Encoding.Default.GetString(decompressedStream.GetBytes());

        // Assert

        // Check that the size is smaller, or at worst, equal
        compressedStreamSize.Should().BeLessThanOrEqualTo(originalStreamSize);
        // Check that the decompression was successful
        originalData.Should().Be(decompressedData);
        // Check the compression mode
        _gzipCompressor.CompressionAlgorithm.Should().Be(CompressionAlgorithm.Zip);
    }

    [Fact]
    public void Gzip_Compression_Implementation_Is_Valid()
    {
        var faker = new Faker();
        var originalData = faker.Lorem.Sentences(10);
        var originalDataBytes = Encoding.Default.GetBytes(originalData);
        var streamToCompress = new MemoryStream(originalDataBytes);

        // Create a memory stream to hold the compressed data
        var compressedStream = new MemoryStream();

        // Use DotNetZip's GZipStream to compress
        using (var gzipStream = new Ionic.Zlib.GZipStream(compressedStream, Ionic.Zlib.CompressionMode.Compress,
                   leaveOpen: true))
        {
            streamToCompress.CopyTo(gzipStream);
        }

        compressedStream.Position = 0;

        // Decompress using GZipStream
        var decompressedStream = new MemoryStream();
        using (var gzipStream = new Ionic.Zlib.GZipStream(compressedStream, Ionic.Zlib.CompressionMode.Decompress))
        {
            gzipStream.CopyTo(decompressedStream);
        }

        // Reset the position of the decompressed stream to the beginning
        decompressedStream.Position = 0;

        var originalFromDotNetZip = decompressedStream.ToArray();

        // Now compress and decompress using our component
        var originalFromComponent = _gzipCompressor.Decompress(_gzipCompressor.Compress(streamToCompress)).GetBytes();
        originalFromDotNetZip.Should().BeEquivalentTo(originalFromComponent);
    }
}