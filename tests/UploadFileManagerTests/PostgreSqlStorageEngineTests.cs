using System.Security.Cryptography;
using System.Text;
using Bogus;
using AwesomeAssertions;
using Microsoft.Extensions.Time.Testing;
using Rad.UploadFileManager;
using Rad.UploadFileManager.StorageEngines;

namespace UploadFileManagerTests;

[Trait("Type", "Integration")]
[Collection("PostgreSQL Collection")]
public class PostgreSqlStorageEngineTests
{
    private readonly UploadFileManager _manager;

    public PostgreSqlStorageEngineTests(PostgreSqlContainerFixture fixture)
    {
        var compressor = new GZipCompressor();

        //
        // Create an encryptor
        //

        // Create Aes object
        var aes = Aes.Create();
        // Create the encryptor
        var encryptor = new AesFileEncryptor(aes.Key, aes.IV);

        // Create the storage engine
        var storageEngine =
            new PostgreSqlStorageEngine(fixture.Container.GetConnectionString(), 5);

        // Create the time provider
        var timeProvider = new FakeTimeProvider();
        timeProvider.SetUtcNow(new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero));

        // Create the file manager
        _manager = new UploadFileManager(storageEngine, encryptor, compressor, timeProvider);
    }

    private static MemoryStream GetFile()
    {
        var faker = new Faker();
        var dataToStore = faker.Lorem.Sentences(20);
        var dataToStoreStream = new MemoryStream(Encoding.UTF8.GetBytes(dataToStore));
        return dataToStoreStream;
    }

    private async Task<FileMetadata> Upload(Stream data)
    {
        return await _manager.UploadFileAsync("Test.txt", ".txt", data, CancellationToken.None);
    }

    [Theory]
    [InlineData(16)]
    [InlineData(32)]
    [InlineData(64)]
    [InlineData(128)]
    [InlineData(256)]
    [InlineData(512)]
    [InlineData(1_000)]
    [InlineData(1_001)]
    [InlineData(1_024)]
    public async Task Large_File_Upload_And_Download_Succeeds(int size)
    {
        // Compute the file size in bytes
        var fileSizeInBytes = 1L * size * 1_024 * 1_024;
        // Create a buffer
        const int bufferSize = 1024 * 1024;

        byte[] buffer = new byte[bufferSize];
        new Random().NextBytes(buffer);

        // Write to a temporary file
        var filePath = Path.GetTempFileName();
        await using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            long bytesWritten = 0;
            while (bytesWritten < fileSizeInBytes)
            {
                long bytesToWrite = Math.Min(bufferSize, fileSizeInBytes - bytesWritten);
                await fs.WriteAsync(buffer.AsMemory(0, (int)bytesToWrite));
                bytesWritten += bytesToWrite;
            }
        }

        // Read the file
        await using (var input = File.OpenRead(filePath))
        {
            // Upload the file
            var uploadMetadata = await Upload(input);
            // Download the file
            await using (var download = await _manager.DownloadFileAsync(uploadMetadata.FileId))
            {
                download.Position = 0;
                // Get the Hash
                var sha = SHA256.Create();
                var currentHash = await sha.ComputeHashAsync(download);
                currentHash.Should().BeEquivalentTo(uploadMetadata.Hash);
            }
        }

        File.Delete(filePath);
    }

    [Fact]
    public async Task Upload_And_Download_Succeeds()
    {
        // Get the data
        var data = GetFile();
        // Upload a file
        var uploadMetadata = await Upload(data);
        // Check the metadata
        uploadMetadata.Should().NotBeNull();
        uploadMetadata.FileId.Should().NotBeEmpty();
        // Download the file
        var download = await _manager.DownloadFileAsync(uploadMetadata.FileId);
        download.GetBytes().Should().BeEquivalentTo(data.GetBytes());
    }

    [Fact]
    public async Task File_Exists_Fails_If_ID_Doesnt_Exist()
    {
        // Check if the file exists
        var result = await _manager.FileExistsAsync(Guid.Empty);
        result.Should().BeFalse();
    }

    [Fact]
    public async Task File_Exists_Fails_If_ID_Exists()
    {
        // Get the data
        var data = GetFile();
        // Upload a file
        var uploadMetadata = await Upload(data);
        // Check if the file exists by ID
        var result = await _manager.FileExistsAsync(uploadMetadata.FileId);
        result.Should().BeTrue();
    }

    [Fact]
    public async Task File_Delete_Succeeds()
    {
        // Get the data
        var data = GetFile();
        // Upload a file
        var uploadMetadata = await Upload(data);
        // Check if the file exists
        var result = await _manager.FileExistsAsync(uploadMetadata.FileId);
        result.Should().BeTrue();
        // Delete the file
        await _manager.DeleteFileAsync(uploadMetadata.FileId);
        // Check again if the file exists
        result = await _manager.FileExistsAsync(uploadMetadata.FileId);
        result.Should().BeFalse();
    }

    [Fact]
    public async Task File_GetMetadata_Succeeds()
    {
        // Get the data
        var data = GetFile();
        // Upload a file
        var uploadMetadata = await Upload(data);
        // Get the metadata from the ID
        var storedMetadata = await _manager.FetchMetadataAsync(uploadMetadata.FileId);
        storedMetadata.Should().NotBeNull();
        storedMetadata.Should().BeEquivalentTo(uploadMetadata);
    }

    [Fact]
    public async Task File_GetMetadata_Fails_If_ID_Doesnt_Exist()
    {
        // Fetch metadata for non-existent ID
        var ex = await Record.ExceptionAsync(() => _manager.FetchMetadataAsync(Guid.Empty));
        ex.Should().BeOfType<FileNotFoundException>();
    }

    [Fact]
    public async Task File_Delete_Fails_If_ID_Doesnt_Exist()
    {
        // Delete a non-existent file id
        var ex = await Record.ExceptionAsync(() => _manager.DeleteFileAsync(Guid.Empty));
        ex.Should().BeOfType<FileNotFoundException>();
    }
}