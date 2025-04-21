using FluentAssertions;
using Moq;
using Rad.UploadFileManager;

namespace UploadFileManagerTests;

public class UploadFileManagerTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("/")]
    [InlineData(@"\Name")]
    [InlineData("/Name")]
    public async Task UploadFile_Throws_Exceptions_For_Invalid_FileName(string? fileName)
    {
        var compressor = new Mock<IFileCompressor>();
        var encryptor = new Mock<IFileEncryptor>();
        var persistor = new Mock<IFilePersistor>();
        var sut = new UploadFileManager(persistor.Object, encryptor.Object, compressor.Object);
        var data = new MemoryStream([0, 3, 3, 4, 4]);
        var ex = await Record.ExceptionAsync(() => sut.UploadFileAsync(fileName, ".png", data));
        ex.Should().BeOfType<ArgumentException>();
    }
}