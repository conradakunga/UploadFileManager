using System.Text.RegularExpressions;

namespace Rad.UploadFileManager;

public static partial class ExtensionValidator
{
    public static void Validate(string extension)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(extension);
        
        // Verify the fileName has valid characters
        char[] customInvalidChars = ['\\', '/'];
        var invalidCharacters = Path.GetInvalidFileNameChars().Union(customInvalidChars).ToArray();

        // Verify the extension has valid characters
        if (invalidCharacters.Any(extension.Contains))
            throw new ArgumentException($"The extension '{extension}' contains invalid characters");

        // Validate the regex for the extension
        if (!ExtensionValidationRegex().IsMatch(extension))
            throw new ArgumentException($"The extension {extension}' does not conform to the expected format: .xxx");
    }

    [GeneratedRegex(@"^\.\w+$")]
    private static partial Regex ExtensionValidationRegex();
}