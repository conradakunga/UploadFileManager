﻿using AwesomeAssertions;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Rad.UploadFileManager;

namespace UploadFileManagerTests;

[Trait("Type", "Behaviour")]
public class UploadFileManagerTests
{
    [Fact]
    public void UploadFile_Throws_Exception_With_Null_Compressor()
    {
        var encryptor = new Mock<IFileEncryptor>(MockBehavior.Strict);
        var persistor = new Mock<IStorageEngine>(MockBehavior.Strict);
        IFileCompressor compressor = null;
        var fakeTimeProvider = new FakeTimeProvider();
        fakeTimeProvider.SetUtcNow(new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var ex = Record.Exception(() =>
            new UploadFileManager(persistor.Object, encryptor.Object, compressor, fakeTimeProvider));
        ex.Should().NotBeNull().And.BeOfType<ArgumentNullException>();
    }

    [Fact]
    public void UploadFile_Throws_Exception_With_Null_Persistor()
    {
        var encryptor = new Mock<IFileEncryptor>(MockBehavior.Strict);
        var compressor = new Mock<IFileCompressor>(MockBehavior.Strict);
        IStorageEngine persistor = null;
        var fakeTimeProvider = new FakeTimeProvider();
        fakeTimeProvider.SetUtcNow(new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var ex = Record.Exception(() =>
            new UploadFileManager(persistor, encryptor.Object, compressor.Object, fakeTimeProvider));
        ex.Should().NotBeNull().And.BeOfType<ArgumentNullException>();
    }

    [Fact]
    public void UploadFile_Throws_Exception_With_Null_Encryptor()
    {
        var compressor = new Mock<IFileCompressor>(MockBehavior.Strict);
        var persistor = new Mock<IStorageEngine>(MockBehavior.Strict);
        IFileEncryptor encryptor = null;
        var fakeTimeProvider = new FakeTimeProvider();
        fakeTimeProvider.SetUtcNow(new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var ex = Record.Exception(() =>
            new UploadFileManager(persistor.Object, encryptor, compressor.Object, fakeTimeProvider));
        ex.Should().NotBeNull().And.BeOfType<ArgumentNullException>();
    }

    [Fact]
    public async Task UploadFileAsync_Invokes_All_Services_Correctly()
    {
        // Setup our sample data
        byte[] originalBytes = [0, 0, 0, 0, 0, 0, 0, 0];
        byte[] compressedBytes = [1, 1, 1, 1];
        byte[] encryptedBytes = [2, 2, 2, 2];

        const string fileName = "Test";
        const string extension = ".doc";

        var metaData = new FileMetadata
        {
            FileId = Guid.Empty,
            Name = fileName,
            Extension = extension,
            DateUploaded = new DateTime(2025, 1, 1, 0, 0, 0),
            OriginalSize = originalBytes.Length,
            PersistedSize = encryptedBytes.Length,
            CompressionAlgorithm = CompressionAlgorithm.None,
            EncryptionAlgorithm = EncryptionAlgorithm.None,
            Hash = [0, 1, 2, 3]
        };

        // Setup our mocks
        var encryptor = new Mock<IFileEncryptor>(MockBehavior.Strict);
        var persistor = new Mock<IStorageEngine>(MockBehavior.Strict);
        var compressor = new Mock<IFileCompressor>(MockBehavior.Strict);

        // Create a sequence to track invocation order
        var sequence = new MockSequence();

        // Configure the behaviour for methods called and properties, specifying the sequence
        compressor.InSequence(sequence).Setup(x => x.Compress(It.IsAny<Stream>()))
            .Returns(new MemoryStream(compressedBytes));
        compressor.Setup(x => x.CompressionAlgorithm)
            .Returns(CompressionAlgorithm.Zip);

        encryptor.InSequence(sequence).Setup(x => x.Encrypt(It.IsAny<Stream>()))
            .Returns(new MemoryStream(encryptedBytes));
        encryptor.Setup(x => x.EncryptionAlgorithm)
            .Returns(EncryptionAlgorithm.Aes);

        persistor.InSequence(sequence).Setup(x =>
                x.StoreFileAsync(It.IsAny<FileMetadata>(), It.IsAny<Stream>(),
                    CancellationToken.None))
            .ReturnsAsync(metaData);

        // Set up the time provider
        var fakeTimeProvider = new FakeTimeProvider();
        fakeTimeProvider.SetUtcNow(new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var manager = new UploadFileManager(persistor.Object, encryptor.Object, compressor.Object, fakeTimeProvider);

        // Invoke the method, discarding the return as validating it is not useful
        _ = await manager.UploadFileAsync(fileName, extension, new MemoryStream(originalBytes), CancellationToken.None);

        // Check that the compressor's Compress method was called once
        compressor.Verify(x => x.Compress(It.IsAny<Stream>()), Times.Once);
        // Check that the encryptor's Encrypt method was called once
        encryptor.Verify(x => x.Encrypt(It.IsAny<Stream>()), Times.Once);
        // Check that the persistor's StoreFileAsync method was called once
        persistor.Verify(
            x => x.StoreFileAsync(It.IsAny<FileMetadata>(), It.IsAny<Stream>(),
                CancellationToken.None),
            Times.Once);
    }
}