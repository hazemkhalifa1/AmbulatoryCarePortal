using System.ComponentModel.DataAnnotations;

namespace AmbulatoryCarePortal.Application.Settings;

public class NotificationSettings
{
    public const string SectionName = "NotificationSettings";

    [Required, MinLength(1)]
    public int[] ExpiryWarningDays { get; set; } = [30, 14, 7];

    [Range(1, 1440)]
    public int CheckIntervalMinutes { get; set; } = 60;
}
