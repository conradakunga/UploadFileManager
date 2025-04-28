using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace Rad.UploadFileManager;

public class SqlWrappedStream : Stream
{
    private readonly Stream _innerStream;
    private readonly SqlConnection _connection;
    private readonly SqlDataReader _reader;

    public SqlWrappedStream(Stream innerStream, SqlConnection connection, SqlDataReader reader)
    {
        _innerStream = innerStream;
        _connection = connection;
        _reader = reader;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _reader.Dispose();
            _connection.Dispose();
        }

        base.Dispose(disposing);
    }

    public override async ValueTask DisposeAsync()
    {
        await _reader.DisposeAsync();
        await _connection.DisposeAsync();
        await base.DisposeAsync();
    }

    public override bool CanRead => _innerStream.CanRead;
    public override bool CanSeek => _innerStream.CanSeek;
    public override bool CanWrite => _innerStream.CanWrite;
    public override long Length => _innerStream.Length;

    public override long Position
    {
        get => _innerStream.Position;
        set => _innerStream.Position = value;
    }

    public override void Flush() => _innerStream.Flush();
    public override int Read(byte[] buffer, int offset, int count) => _innerStream.Read(buffer, offset, count);
    public override long Seek(long offset, SeekOrigin origin) => _innerStream.Seek(offset, origin);
    public override void SetLength(long value) => _innerStream.SetLength(value);
    public override void Write(byte[] buffer, int offset, int count) => _innerStream.Write(buffer, offset, count);
}