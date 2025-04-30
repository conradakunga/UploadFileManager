using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Rad.UploadFileManager;

/// <summary>
/// Sql Server Storage Engine
/// </summary>
public sealed class SqlServerStorageEngine : IStorageEngine
{
    private readonly string _connectionString;

    /// <summary>
    /// Constructor, taking the connection string
    /// </summary>
    /// <param name="connectionString"></param>
    public SqlServerStorageEngine(string connectionString)
    {
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
        // Create and initialize command
        var param = new DynamicParameters();
        param.Add("FileID", metaData.FileId, DbType.Guid);
        param.Add("Name", metaData.Name, DbType.String, size: 500);
        param.Add("Extension", metaData.Extension, DbType.String, size: 10);
        param.Add("DateUploaded", metaData.DateUploaded, DbType.DateTime2);
        param.Add("OriginalSize", metaData.OriginalSize, DbType.Int32);
        param.Add("PersistedSize", metaData.PersistedSize, DbType.Int32);
        param.Add("CompressionAlgorithm", metaData.CompressionAlgorithm, DbType.Byte);
        param.Add("EncryptionAlgorithm", metaData.EncryptionAlgorithm, DbType.Byte);
        param.Add("Hash", metaData.Hash, dbType: DbType.Binary, size: 32);
        param.Add("Data", data, DbType.Binary, size: -1);
        var command = new CommandDefinition(sql, param, cancellationToken: cancellationToken);
        await using (var cn = new SqlConnection(_connectionString))
        {
            await cn.ExecuteAsync(command);
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
        await using (var cn = new SqlConnection(_connectionString))
        {
            return await cn.QuerySingleAsync<FileMetadata>(command);
        }
    }

    /// <inheritdoc />
    public async Task<Stream> GetFileAsync(Guid fileId, CancellationToken cancellationToken = default)
    {
        // Query to fetch file
        const string sql = "SELECT Data FROM Files where FileId = @FileId";
        // Create and initialize command
        var command = new CommandDefinition(sql, new { FileId = fileId }, cancellationToken: cancellationToken);
        await using (var cn = new SqlConnection(_connectionString))
        {
            var result = await cn.QuerySingleAsync<byte[]>(command);
            return new MemoryStream(result);
        }
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