namespace Rad.UploadFileManager;

/// <summary>
/// File compression & decompression contract
/// </summary>
public interface IFileCompressor
{
    /// <summary>
    /// Compression algorithm to use
    /// </summary>
    public CompressionAlgorithm CompressionAlgorithm { get; }

    /// <summary>
    /// Compress stream
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public Stream Compress(Stream data);

    /// <summary>
    /// De-compress the stream
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public Stream Decompress(Stream data);
}