using System.ComponentModel.DataAnnotations;

namespace AmbulatoryCarePortal.Application.Settings;

public class RedisSettings
{
    public const string SectionName = "Redis";

    public string ConnectionString { get; set; } = string.Empty;
}
