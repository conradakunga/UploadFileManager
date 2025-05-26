using System.Data;
using Dapper;
using Npgsql;
using NpgsqlTypes;

namespace Rad.UploadFileManager.StorageEngines;

/// <summary>
/// PostgreSQL Storage Engine
/// </summary>
public sealed class PostgreSqlStorageEngine : IStorageEngine
{
    /// <inheritdoc/> 
    public int TimeoutInMinutes { get; }

    private readonly string _connectionString;

    /// <summary>
    /// Constructor, taking the connection string
    /// </summary>
    /// <param name="connectionString"></param>
    /// <param name="timeoutInMinutes"></param>
    public PostgreSqlStorageEngine(string connectionString, int timeoutInMinutes)
    {
        TimeoutInMinutes = timeoutInMinutes;
        _connectionString = connectionString;
        // Parse the connection string for a database
        var parser = new PostgreSqlConnectionStringParser(connectionString);
        if (string.IsNullOrEmpty(parser.Database))
            throw new ArgumentException($"{nameof(parser.Database)} cannot be null or empty");
    }

    /// <inheritdoc />
    public async Task<FileMetadata> StoreFileAsync(FileMetadata metaData, Stream data,
        CancellationToken cancellationToken = default)
    {
        // Check the stream size
        if (data.Length > Constants.PostgreSQLLargeObjectThreshold)
        {
            // This branch is for streams LARGER than the threshold
            await using (var cn = new NpgsqlConnection(_connectionString))
            {
                await cn.OpenAsync(cancellationToken);
                var trans = await cn.BeginTransactionAsync(cancellationToken);
#pragma warning disable CS0618 // Type or member is obsolete
                var loManager = new NpgsqlLargeObjectManager(cn);
#pragma warning restore CS0618 // Type or member is obsolete

                // Create a new large object OID
                var oid = loManager.Create();

                // Open the LOB stream for writing
                await using (var loStream = await loManager.OpenReadWriteAsync(oid, cancellationToken))
                {
                    // Copy your data stream into the large object stream
                    data.Position = 0;
                    var buffer = new byte[Constants.DefaultBufferSize];
                    int bytesRead;

                    while ((bytesRead = await data.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                    {
                        await loStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                    }

                    await loStream.FlushAsync(cancellationToken);
                }

                // Query to insert file metadata
                const string sql = """
                                   INSERT INTO Files (
                                              FileID, Name, Extension, DateUploaded,
                                              OriginalSize, PersistedSize, CompressionAlgorithm,
                                              EncryptionAlgorithm, Hash, Data, Loid 
                                          ) VALUES (
                                              @FileID, @Name, @Extension, @DateUploaded,
                                              @OriginalSize, @PersistedSize, @CompressionAlgorithm,
                                              @EncryptionAlgorithm, @Hash, @Data, @Loid
                                          )
                                   """;

                var cmd = new NpgsqlCommand(sql, cn, trans);
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
                cmd.Parameters.AddWithValue("@Data", NpgsqlDbType.Bytea,
                    new MemoryStream(Constants.PostgreSQLLargeObjectMaker));
                cmd.Parameters.AddWithValue("@Loid", NpgsqlDbType.Oid, oid);

                await cmd.ExecuteNonQueryAsync(cancellationToken);

                await trans.CommitAsync(cancellationToken);
            }
        }
        else
        {
            // This branch is for streams equal to or less than the threshold
            
            // Query to insert file metadata
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

            var cmd = new NpgsqlCommand(sql, cn);
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
            var dataParam = new NpgsqlParameter("@Data", NpgsqlDbType.Bytea)
            {
                Value = data,
                Size = -1
            };
            cmd.Parameters.Add(dataParam);

            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

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
        // Get local metadata
        var sql = "Select loid from Files where FileId = @FileId";
        uint? oid = null;
        await using (var cn = new NpgsqlConnection(_connectionString))
        {
            oid = await cn.QuerySingleOrDefaultAsync<uint?>(sql, new { FileId = fileId });
        }

        if (oid.HasValue)
        {
            await using (var cn = new NpgsqlConnection(_connectionString))
            {
                await cn.OpenAsync(cancellationToken);
                await using (var trans = await cn.BeginTransactionAsync(cancellationToken))
                {
                    // Fetch and return the lob
#pragma warning disable CS0618 // Type or member is obsolete
                    var loManager = new NpgsqlLargeObjectManager(cn);
#pragma warning restore CS0618 // Type or member is obsolete
                    uint id = oid.Value;
                    await using (var stream = await loManager.OpenReadAsync(id, cancellationToken))
                    {
                        var memoryStream = new MemoryStream();
                        var buffer = new byte[Constants.DefaultBufferSize];

                        int bytesRead;

                        while ((bytesRead = await stream.ReadAsync(buffer, cancellationToken)) > 0)
                        {
                            await memoryStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                        }

                        // Close the large object stream
                        stream.Close();

                        await trans.CommitAsync(cancellationToken);

                        // Reset the position
                        memoryStream.Position = 0;

                        return memoryStream;
                    }
                }
            }
        }

        // If we are here, there was no OID. Query to fetch file
        sql = "SELECT Data FROM Files where FileId = @FileId";
        await using (var cn = new NpgsqlConnection(_connectionString))
        {
            await using (var cmd = new NpgsqlCommand(sql, cn))
            {
                await cn.OpenAsync(cancellationToken);

                // Increase the timout in case of large files
                cmd.CommandTimeout = (int)TimeSpan.FromMinutes(TimeoutInMinutes).TotalSeconds;
                cmd.Parameters.AddWithValue("FileID", fileId);

                await using (var reader =
                             await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken))
                {
                    if (await reader.ReadAsync(cancellationToken))
                    {
                        var memoryStream = new MemoryStream();
                        await using (var dataStream =
                                     await reader.GetStreamAsync(0, cancellationToken).ConfigureAwait(false))
                            await dataStream.CopyToAsync(memoryStream, Constants.DefaultBufferSize,
                                cancellationToken);

                        memoryStream.Position = 0;

                        return memoryStream;
                    }
                }
            }
        }

        // If we are here, could not get the file data
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