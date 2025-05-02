namespace Rad.UploadFileManager;

public static class StreamExtensions
{
    /// <summary>
    /// Given a stream, get a byte array
    /// </summary>
    /// <param name="stream"></param>
    /// <returns></returns>
    public static byte[] GetBytes(this Stream stream)
    {
        using (var memoryStream = new MemoryStream())
        {
            stream.CopyTo(memoryStream);
            // Reset the stream position
            stream.Position = 0;
            return memoryStream.ToArray();
        }
    }
}