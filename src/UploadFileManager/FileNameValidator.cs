namespace Rad.UploadFileManager;

public static class FileNameValidator
{
    public static void Validate(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        // Verify the fileName has valid characters
        char[] customInvalidChars = ['\\', '/'];
        var invalidCharacters = Path.GetInvalidFileNameChars().Union(customInvalidChars).ToArray();
        if (invalidCharacters.Any(fileName.Contains))
            throw new ArgumentException($"The file name '{fileName}' contains invalid characters");
    }
}