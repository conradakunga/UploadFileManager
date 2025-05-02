using System.Data;
using Dapper;
using Npgsql;
using NpgsqlTypes;

namespace Rad.UploadFileManager;

/// <summary>
/// PostgreSQL Storage Engine
/// </summary>
public sealed class PosgrgreSQLStorageEngine : IStorageEngine
{
    /// <inheritdoc/> 
    public int TimeoutInMinutes { get; }

    private readonly string _connectionString;

    /// <summary>
    /// Constructor, taking the connection string
    /// </summary>
    /// <param name="connectionString"></param>
    /// <param name="timeoutInMinutes"></param>
    public PosgrgreSQLStorageEngine(string connectionString, int timeoutInMinutes)
    {
        TimeoutInMinutes = timeoutInMinutes;
        _connectionString = connectionString;
        // Parse the connection string for a database
        var parser = new PostgreSQLConnectionStringParser(connectionString);
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
        await using var cn = new NpgsqlConnection(_connectionString);
        await cn.OpenAsync(cancellationToken);

        await using var cmd = new NpgsqlCommand(sql, cn);
        cmd.CommandTimeout = (int)TimeSpan.FromMinutes(TimeoutInMinutes).TotalSeconds;

        // Add normal parameters
        cmd.Parameters.AddWithValue("@FileId", NpgsqlDbType.Uuid, metaData.FileId);
        cmd.Parameters.AddWithValue("@Name", NpgsqlDbType.Varchar, metaData.Name);
        cmd.Parameters.AddWithValue("@Extension", NpgsqlDbType.Varchar, metaData.Extension);
        cmd.Parameters.AddWithValue("@DateUploaded", NpgsqlDbType.TimestampTz, metaData.DateUploaded);
        cmd.Parameters.AddWithValue("@OriginalSize", NpgsqlDbType.Integer, metaData.OriginalSize);
        cmd.Parameters.AddWithValue("@PersistedSize", NpgsqlDbType.Integer, metaData.PersistedSize);
        cmd.Parameters.AddWithValue("@CompressionAlgorithm", NpgsqlDbType.Smallint,
            (byte)metaData.CompressionAlgorithm);
        cmd.Parameters.AddWithValue("@EncryptionAlgorithm", NpgsqlDbType.Smallint,
            (byte)metaData.EncryptionAlgorithm);
        cmd.Parameters.AddWithValue("@Hash", NpgsqlDbType.Bytea, metaData.Hash);

        // Stream parameter
        data.Position = 0;
        var dataParam = new NpgsqlParameter("Data", NpgsqlDbType.Bytea)
        {
            Value = data,
            Size = -1
        };
        cmd.Parameters.Add(dataParam);

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
        await using (var cn = new NpgsqlConnection(_connectionString))
        {
            return await cn.QuerySingleAsync<FileMetadata>(command);
        }
    }

    /// <inheritdoc />
    public async Task<Stream> GetFileAsync(Guid fileId, CancellationToken cancellationToken = default)
    {
        // Query to fetch file
        const string sql = "SELECT Data FROM Files where FileId = @FileId";

        await using (var cn = new NpgsqlConnection(_connectionString))
        {
            await cn.OpenAsync(cancellationToken);

            await using (var cmd = new NpgsqlCommand(sql, cn))
            {
                // Increase the timout in case of large files
                cmd.CommandTimeout = (int)TimeSpan.FromMinutes(5).TotalSeconds;
                cmd.Parameters.AddWithValue("FileID", fileId);

                await using (var reader =
                             await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken))
                {
                    if (await reader.ReadAsync(cancellationToken))
                    {
                        var memoryStream = new MemoryStream();
                        await using (var dataStream =
                                     await reader.GetStreamAsync(0, cancellationToken).ConfigureAwait(false))
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
        await using (var cn = new NpgsqlConnection(_connectionString))
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
        await using (var cn = new NpgsqlConnection(_connectionString))
        {
            return await cn.QuerySingleOrDefaultAsync<int?>(command) != null;
        }
    }
}