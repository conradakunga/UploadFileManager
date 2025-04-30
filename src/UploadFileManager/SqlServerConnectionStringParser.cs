using System.Text.RegularExpressions;

namespace Rad.UploadFileManager;

public sealed partial class SqlServerConnectionStringParser
{
    private readonly string _connectionString;

    public SqlServerConnectionStringParser(string connectionString)
    {
        _connectionString = connectionString;
    }

    public string Database => DatabaseRegex().Match(_connectionString).Groups["database"].Value.Trim();

    [GeneratedRegex(@"(Database|Initial\s+Catalog)\s*=\s*(?<database>.*?\s*)(;|$)", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex DatabaseRegex();
}