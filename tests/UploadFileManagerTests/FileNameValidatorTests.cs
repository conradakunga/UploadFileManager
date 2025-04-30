using FluentAssertions;
using Rad.UploadFileManager;

namespace UploadFileManagerTests;

[Trait("Type", "Unit")]
public class FileNameValidatorTests
{
    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("/")]
    public void FileNameValidator_Throws_Exception_If_Name_Is_Not_Valid(string name)
    {
        var ex = Record.Exception(() => FileNameValidator.Validate(name));
        ex.Should().BeOfType<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    public void FileNameValidator_Throws_Exception_If_Name_Is_Null(string name)
    {
        var ex = Record.Exception(() => FileNameValidator.Validate(name));
        ex.Should().BeOfType<ArgumentNullException>();
    }

    [Theory]
    [InlineData(@"\Name")]
    [InlineData("/Name")]
    public void FileNameValidator_Throws_Exception_If_Name_Has_Invalid_Character(string name)
    {
        var ex = Record.Exception(() => FileNameValidator.Validate(name));
        ex.Should().BeOfType<ArgumentException>();
    }
}