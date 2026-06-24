using System.ComponentModel.DataAnnotations;

namespace AmbulatoryCarePortal.Application.Settings;

public class FileUploadSettings
{
    public const string SectionName = "FileUploadSettings";

    [Range(1, 104857600)]
    public long MaxFileSizeBytes { get; set; } = 20971520;

    [Required, MinLength(1)]
    public string[] AllowedExtensions { get; set; } = [];

    [Required]
    public string BasePath { get; set; } = "wwwroot/uploads";
}
