using System.ComponentModel.DataAnnotations;

namespace Rad.UploadFileManager;

public class GoogleSettings
{
    [Required] public string ProjectID { get; set; } = null!;
    [Required] public string BucketLocation { get; set; } = null!;
}