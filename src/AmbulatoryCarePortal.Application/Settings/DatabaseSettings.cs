using System.ComponentModel.DataAnnotations;

namespace AmbulatoryCarePortal.Application.Settings;

public class DatabaseSettings
{
    public const string SectionName = "ConnectionStrings";

    public string DefaultConnection { get; set; } = string.Empty;
}
