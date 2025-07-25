using System.ComponentModel.DataAnnotations;

namespace Rad.UploadFileManager;

public class AmazonSettings
{
    [Required] public string AccountName { get; set; } = null!;
    [Required] public string AccountKey { get; set; } = null!;
    [Required] public string AzureLocation { get; set; } = null!;
    [Required] public string DataContainerName { get; set; } = null!;
    [Required] public string MetadataContainerName { get; set; } = null!;
}