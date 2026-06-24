using System.ComponentModel.DataAnnotations;

namespace AmbulatoryCarePortal.Application.Settings;

public class SecuritySettings
{
    public const string SectionName = "Security";

    public string AdminPassword { get; set; } = string.Empty;
}
