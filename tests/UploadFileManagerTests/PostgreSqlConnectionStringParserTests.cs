using FluentAssertions;
using Rad.UploadFileManager;

namespace UploadFileManagerTests;

[Trait("Type", "Unit")]
public class PostgreSqlConnectionStringParserTests
{
    [Theory]
    [InlineData("host=server;username=login;Password=pass;Database=database", "database")]
    [InlineData("host=server;username=login;Password=pass;Database=database ", "database")]
    [InlineData("host=server;username=login;Password=pass;database=my_database ", "my_database")]
    [InlineData("Database=database;host=server;username=login;Password=pass;", "database")]
    [InlineData("database=my_database;host=server;username=login;Password=pass;", "my_database")]
    [InlineData("host=server;username=login;Password=pass;", null)]
    public void Database_Name_Is_Parsed(string connectionString, string? databaseName)
    {
        var parser = new PostgreSqlConnectionStringParser(connectionString);
        parser.Database.Should().Be(databaseName);
    }
}