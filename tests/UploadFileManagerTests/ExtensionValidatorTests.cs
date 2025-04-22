using FluentAssertions;
using Rad.UploadFileManager;

namespace UploadFileManagerTests;

public class ExtensionValidatorTests
{
    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("/")]
    public void ExtensionValidator_Throws_Exception_If_Name_Is_Not_Valid(string name)
    {
        var ex = Record.Exception(() => ExtensionValidator.Validate(name));
        ex.Should().BeOfType<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    public void ExtensionValidator_Throws_Exception_If_Name_Is_Null(string name)
    {
        var ex = Record.Exception(() => ExtensionValidator.Validate(name));
        ex.Should().BeOfType<ArgumentNullException>();
    }

    [Theory]
    [InlineData(@".\Name")]
    [InlineData("./Name")]
    public void ExtensionValidator_Throws_Exception_If_Name_Has_Invalid_Character(string name)
    {
        var ex = Record.Exception(() => ExtensionValidator.Validate(name));
        ex.Should().BeOfType<ArgumentException>();
    }

    [Theory]
    [InlineData(@".doc.")]
    [InlineData("..doc")]
    public void ExtensionValidator_Throws_Exception_If_Name_Has_Invalid_Format(string name)
    {
        var ex = Record.Exception(() => ExtensionValidator.Validate(name));
        ex.Should().BeOfType<ArgumentException>();
    }
}