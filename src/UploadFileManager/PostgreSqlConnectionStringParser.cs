using Npgsql;

namespace Rad.UploadFileManager;

public sealed class PostgreSqlConnectionStringParser
{
    private readonly string _connectionString;

    public PostgreSqlConnectionStringParser(string connectionString)
    {
        _connectionString = connectionString;
    }

    public string? Database
    {
        get
        {
            var builder = new NpgsqlConnectionStringBuilder(_connectionString);
            return builder.Database;
        }
    }
}