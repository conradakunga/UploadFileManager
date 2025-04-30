using FluentAssertions;
using Rad.UploadFileManager;

namespace UploadFileManagerTests;

[Trait("Type", "Unit")]
public class SqlServerConnectionStringParserTests
{
    [Theory]
    [InlineData("Host=server;Username=login;Password=pass;Database=database", "database")]
    [InlineData("Host=server;Username=login;Password=pass;database=my_database", "my_database")]
    [InlineData("Host=server;Username=login;Password=pass;Database=database ", "database")]
    [InlineData("Host=server;Username=login;Password=pass;database=my_database ", "my_database")]
    [InlineData("Host=server;Username=login;Password=pass;Initial  Catalog=database", "database")]
    [InlineData("Host=server;Username=login;Password=pass;Initial Catalog=my_database", "my_database")]
    [InlineData("Database=database;Host=server;Username=login;Password=pass;", "database")]
    [InlineData("database=my_database;Host=server;Username=login;Password=pass;", "my_database")]
    [InlineData("Initial Catalog=database;Host=server;Username=login;Password=pass;", "database")]
    [InlineData("Initial Catalog=my_database;Host=server;Username=login;Password=pass;", "my_database")]
    [InlineData("Host=server;Username=login;Password=pass;", "")]
    public void Database_Name_Is_Parsed(string connectionString, string databaseName)
    {
        var parser = new SqlServerConnectionStringParser(connectionString);
        parser.Database.Should().Be(databaseName);
    }
}