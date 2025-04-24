using System.IO.Compression;

namespace Rad.UploadFileManager;

/// <summary>
/// Component that zips and unzips streams
/// </summary>
public sealed class GZipCompressor : IFileCompressor
{
    /// <summary>
    /// Return the compression algorithm in use - Zip
    /// </summary>
    public CompressionAlgorithm CompressionAlgorithm => CompressionAlgorithm.Zip;

    /// <inheritdoc />
    public Stream Compress(Stream data)
    {
        // Ensure the stream is positioned at start
        data.Position = 0;

        // Create a memory stream to hold the compressed data
        var compressedStream = new MemoryStream();

        // Use GZipStream to compress the data. Leave open so that
        // the downstream components can use it
        using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Compress, leaveOpen: true))
        {
            // Copy the input stream into the GZip stream
            data.CopyTo(gzipStream);
        }

        // Set the position of the compressed stream to the beginning
        compressedStream.Position = 0;

        return compressedStream;
    }

    /// <inheritdoc />
    public Stream Decompress(Stream data)
    {
        // Ensure the stream is positioned at start
        data.Position = 0;

        // Create a memory stream to hold the decompressed data
        var decompressedStream = new MemoryStream();

        // Use GZipStream to decompress the data. Leave open so that
        // the downstream components can use it
        using (var gzipStream = new GZipStream(data, CompressionMode.Decompress))
        {
            // Copy the decompressed data into the memory stream
            gzipStream.CopyTo(decompressedStream);
        }

        // Set the position of the decompressed stream to the beginning
        decompressedStream.Position = 0;

        return decompressedStream;
    }
}