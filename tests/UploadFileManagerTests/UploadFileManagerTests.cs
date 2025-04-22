using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Rad.UploadFileManager;

namespace UploadFileManagerTests;

public class UploadFileManagerTests
{
    [Fact]
    public void UploadFile_Throws_Exception_With_Null_Compressor()
    {
        var encryptor = new Mock<IFileEncryptor>(MockBehavior.Strict);
        var persistor = new Mock<IFilePersistor>(MockBehavior.Strict);
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
        IFilePersistor persistor = null;
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
        var persistor = new Mock<IFilePersistor>(MockBehavior.Strict);
        IFileEncryptor encryptor = null;
        var fakeTimeProvider = new FakeTimeProvider();
        fakeTimeProvider.SetUtcNow(new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var ex = Record.Exception(() =>
            new UploadFileManager(persistor.Object, encryptor, compressor.Object, fakeTimeProvider));
        ex.Should().NotBeNull().And.BeOfType<ArgumentNullException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("/")]
    [InlineData(@"\Name")]
    [InlineData("/Name")]
    public async Task UploadFile_Throws_Exceptions_For_Invalid_FileName(string? fileName)
    {
        var compressor = new Mock<IFileCompressor>(MockBehavior.Strict);
        var encryptor = new Mock<IFileEncryptor>(MockBehavior.Strict);
        var persistor = new Mock<IFilePersistor>(MockBehavior.Strict);
        var fakeTimeProvider = new FakeTimeProvider();
        fakeTimeProvider.SetUtcNow(new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var sut = new UploadFileManager(persistor.Object, encryptor.Object, compressor.Object, fakeTimeProvider);
        var data = new MemoryStream([0, 3, 3, 4, 4]);
        var ex = await Record.ExceptionAsync(() => sut.UploadFileAsync(fileName, ".png", data));
        ex.Should().BeOfType<ArgumentException>();
    }
}