using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace Rad.UploadFileManager;

public sealed partial class SqlServerConnectionStringParser
{
    private readonly string _connectionString;

    public SqlServerConnectionStringParser(string connectionString)
    {
        _connectionString = connectionString;
    }

    public string Database
    {
        get
        {
            var builder = new SqlConnectionStringBuilder(_connectionString);
            return builder.InitialCatalog;
        }
    }
}