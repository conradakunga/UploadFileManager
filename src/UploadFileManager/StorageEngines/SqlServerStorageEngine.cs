using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Rad.UploadFileManager.StorageEngines;

/// <summary>
/// Sql Server Storage Engine
/// </summary>
public sealed class SqlServerStorageEngine : IStorageEngine
{
    /// <inheritdoc />
    public int TimeoutInMinutes { get; }

    private readonly string _connectionString;

    /// <summary>
    /// Constructor, taking the connection string and timeout
    /// </summary>
    /// <param name="connectionString"></param>
    /// <param name="timeoutInMinutes"></param>
    public SqlServerStorageEngine(string connectionString, int timeoutInMinutes)
    {
        TimeoutInMinutes = timeoutInMinutes;
        _connectionString = connectionString;
        // Parse the connection string
        var parser = new SqlServerConnectionStringParser(connectionString);
        if (string.IsNullOrEmpty(parser.Database))
            throw new ArgumentException($"{nameof(parser.Database)} cannot be null or empty");
    }

    /// <inheritdoc />
    public async Task<FileMetadata> StoreFileAsync(FileMetadata metaData, Stream data,
        CancellationToken cancellationToken = default)
    {
        // Query to fetch file metadata
        const string sql = """
                           INSERT INTO Files (
                                      FileID, Name, Extension, DateUploaded,
                                      OriginalSize, PersistedSize, CompressionAlgorithm,
                                      EncryptionAlgorithm, Hash, Data
                                  ) VALUES (
                                      @FileID, @Name, @Extension, @DateUploaded,
                                      @OriginalSize, @PersistedSize, @CompressionAlgorithm,
                                      @EncryptionAlgorithm, @Hash, @Data
                                  )
                           """;
        data.Position = 0;

        // Set up command
        await using var cn = new SqlConnection(_connectionString);
        await cn.OpenAsync(cancellationToken);

        await using var cmd = new SqlCommand(sql, cn);
        cmd.CommandTimeout = (int)TimeSpan.FromMinutes(TimeoutInMinutes).TotalSeconds;

        cmd.Parameters.Add(new SqlParameter("@FileID", SqlDbType.UniqueIdentifier) { Value = metaData.FileId });
        cmd.Parameters.Add(new SqlParameter("@Name", SqlDbType.NVarChar, 500) { Value = metaData.Name });
        cmd.Parameters.Add(new SqlParameter("@Extension", SqlDbType.NVarChar, 10) { Value = metaData.Extension });
        cmd.Parameters.Add(
            new SqlParameter("@DateUploaded", SqlDbType.DateTimeOffset) { Value = metaData.DateUploaded });
        cmd.Parameters.Add(new SqlParameter("@OriginalSize", SqlDbType.Int) { Value = metaData.OriginalSize });
        cmd.Parameters.Add(new SqlParameter("@PersistedSize", SqlDbType.Int) { Value = metaData.PersistedSize });
        cmd.Parameters.Add(new SqlParameter("@CompressionAlgorithm", SqlDbType.TinyInt)
            { Value = metaData.CompressionAlgorithm });
        cmd.Parameters.Add(new SqlParameter("@EncryptionAlgorithm", SqlDbType.TinyInt)
            { Value = metaData.EncryptionAlgorithm });
        cmd.Parameters.Add(new SqlParameter("@Hash", SqlDbType.Binary, 32) { Value = metaData.Hash });

        var dataParam = new SqlParameter("@Data", SqlDbType.VarBinary, -1)
        {
            Value = data,
            Direction = ParameterDirection.Input
        };
        cmd.Parameters.Add(dataParam);

        // Execute
        await cmd.ExecuteNonQueryAsync(cancellationToken);

        return metaData;
    }

    /// <inheritdoc />
    public async Task<FileMetadata> GetMetadataAsync(Guid fileId, CancellationToken cancellationToken = default)
    {
        // Query to fetch file metadata
        const string sql =
            "SELECT FileID, Name, Extension, DateUploaded, OriginalSize, PersistedSize, CompressionAlgorithm, EncryptionAlgorithm, Hash FROM Files where FileId = @FileId";
        // Create and initialize command
        var command = new CommandDefinition(sql, new { FileId = fileId }, cancellationToken: cancellationToken);
        await using (var cn = new SqlConnection(_connectionString))
        {
            return await cn.QuerySingleAsync<FileMetadata>(command);
        }
    }

    /// <inheritdoc />
    public async Task<Stream> GetFileAsync(Guid fileId, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT Data FROM Files WHERE FileId = @FileId";

        await using (var cn = new SqlConnection(_connectionString))
        {
            await cn.OpenAsync(cancellationToken);

            await using (var cmd = new SqlCommand(sql, cn))
            {
                // Increase the timout in case of large files
                cmd.CommandTimeout = (int)TimeSpan.FromMinutes(TimeoutInMinutes).TotalSeconds;
                cmd.Parameters.AddWithValue("@FileId", fileId);

                await using (var reader =
                             await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken))
                {
                    if (await reader.ReadAsync(cancellationToken))
                    {
                        var memoryStream = new MemoryStream();

                        await using (var dataStream = reader.GetStream(0))
                            await dataStream.CopyToAsync(memoryStream, Constants.DefaultBufferSize, cancellationToken);

                        memoryStream.Position = 0;

                        return memoryStream;
                    }
                }
            }
        }

        throw new FileNotFoundException($"The file '{fileId}' was not found");
    }

    /// <inheritdoc />
    public async Task DeleteFileAsync(Guid fileId, CancellationToken cancellationToken = default)
    {
        // Query to delete file
        const string sql = "DELETE FROM Files where FileId = @FileId";
        // Create and initialize command
        var command = new CommandDefinition(sql, new { FileId = fileId }, cancellationToken: cancellationToken);
        await using (var cn = new SqlConnection(_connectionString))
        {
            await cn.ExecuteAsync(command);
        }
    }

    /// <inheritdoc />
    public async Task<bool> FileExistsAsync(Guid fileId, CancellationToken cancellationToken = default)
    {
        // Query to check for file existence
        const string sql = "SELECT 1 FROM Files where FileId = @FileId";
        // Create and initialize command
        var command = new CommandDefinition(sql, new { FileId = fileId }, cancellationToken: cancellationToken);
        await using (var cn = new SqlConnection(_connectionString))
        {
            return await cn.QuerySingleOrDefaultAsync<int?>(command) != null;
        }
    }
}