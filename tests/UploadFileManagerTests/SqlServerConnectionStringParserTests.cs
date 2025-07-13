using AwesomeAssertions;
using Rad.UploadFileManager;

namespace UploadFileManagerTests;

[Trait("Type", "Unit")]
public class SqlServerConnectionStringParserTests
{
    [Theory]
    [InlineData("data source=server;uid=login;Password=pass;Database=database", "database")]
    [InlineData("data source=server;uid=login;Password=pass;Database=database ", "database")]
    [InlineData("data source=server;uid=login;Password=pass;database=my_database ", "my_database")]
    [InlineData("data source=server;uid=login;Password=pass;Initial Catalog=database", "database")]
    [InlineData("data source=server;uid=login;Password=pass;Initial Catalog=my_database", "my_database")]
    [InlineData("Database=database;data source=server;uid=login;Password=pass;", "database")]
    [InlineData("database=my_database;data source=server;uid=login;Password=pass;", "my_database")]
    [InlineData("Initial Catalog=database;data source=server;uid=login;Password=pass;", "database")]
    [InlineData("Initial Catalog=my_database;data source=server;uid=login;Password=pass;", "my_database")]
    [InlineData("data source=server;uid=login;Password=pass;", "")]
    public void Database_Name_Is_Parsed(string connectionString, string databaseName)
    {
        var parser = new SqlServerConnectionStringParser(connectionString);
        parser.Database.Should().Be(databaseName);
    }
}