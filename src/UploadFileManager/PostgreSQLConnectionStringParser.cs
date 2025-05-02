using Npgsql;

namespace Rad.UploadFileManager;

public sealed class PostgreSQLConnectionStringParser
{
    private readonly string _connectionString;

    public PostgreSQLConnectionStringParser(string connectionString)
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