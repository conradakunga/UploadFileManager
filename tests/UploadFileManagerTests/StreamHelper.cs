namespace UploadFileManagerTests;

public static class StreamHelper
{
    public static bool Matches(this Stream stream, byte[] expectedBytes)
    {
        if (!stream.CanSeek) return false;

        stream.Position = 0;
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        var actualBytes = ms.ToArray();

        return actualBytes.SequenceEqual(expectedBytes);
    }
}