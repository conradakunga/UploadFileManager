using System.ComponentModel.DataAnnotations;

namespace Rad.UploadFileManager;

public class AzureSettings
{
    [Required] public string AccountName { get; set; } = null!;
    [Required] public string DataContainerName { get; set; } = null!;
    [Required] public string MetadataContainerName { get; set; } = null!;
}