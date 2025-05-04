using Dapper;
using Npgsql;
using Testcontainers.PostgreSql;

namespace UploadFileManagerTests;

public sealed class PostgreSqlContainerFixture : IAsyncLifetime
{
    // Instance of the database
    public readonly PostgreSqlContainer Container = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .WithDatabase("FileStore")
        .Build();

    private async Task InitializeDatabaseAsync()
    {
        var queryText = await File.ReadAllTextAsync("PostgreSQLSetup.sql");
        // Execute
        await using (var cn = new NpgsqlConnection(Container.GetConnectionString()))
        {
            await cn.ExecuteAsync(queryText);
        }
    }

    public async Task InitializeAsync()
    {
        // Start the database
        await Container.StartAsync();

        // Initialize the database
        await InitializeDatabaseAsync();
    }

    public Task DisposeAsync()
    {
        return Container.DisposeAsync().AsTask();
    }
}