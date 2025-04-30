using Dapper;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

namespace UploadFileManagerTests;

public class SqlServerContainerFixture : IAsyncLifetime
{
    private const string DatabaseName = "FileStore";

    // Instance of the database
    public readonly MsSqlContainer Container = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    private async Task InitializeDatabaseAsync()
    {
        var queryText = await File.ReadAllTextAsync("SqlServerSetup.sql");
        // Execute
        await using (var cn = new SqlConnection(Container.GetConnectionString()))
        {
            await cn.ExecuteAsync(queryText);
        }
    }

    public async Task InitializeAsync()
    {
        // Start the database
        await Container.StartAsync();

        // After starting, create the database manually
        await using var connection = new SqlConnection(Container.GetConnectionString());
        await connection.OpenAsync();

        const string sql = $"""
                            IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = '{DatabaseName}')
                            BEGIN
                                CREATE DATABASE [{DatabaseName}];
                            END
                            """;

        await connection.ExecuteAsync(sql);

        // Initialize the database
        await InitializeDatabaseAsync();
    }

    public Task DisposeAsync()
    {
        return Container.DisposeAsync().AsTask();
    }
}