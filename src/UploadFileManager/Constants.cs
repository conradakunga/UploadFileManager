namespace Rad.UploadFileManager;

public static class Constants
{
    public const int DefaultBufferSize = 80 * 1_024;
    public const long PostgreSQLLargeObjectThreshold = 1_000 * 1_024 * 1_024;
    public static readonly byte[] PostgreSQLLargeObjectMaker = [0, 0, 0];
}