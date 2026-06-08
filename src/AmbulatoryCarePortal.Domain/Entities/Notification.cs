using AmbulatoryCarePortal.Domain.Enums;

namespace AmbulatoryCarePortal.Domain.Entities;

public class Notification : BaseEntity
{
    public int ClinicId { get; set; }
    public string? Title { get; set; }
    public string? Message { get; set; }
    public string? MessageAr { get; set; }
    public NotificationType NotificationType { get; set; }
    public int? TargetObjectId { get; set; }
    public string TargetObjectType { get; set; } = string.Empty;
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }
    public string? UserId { get; set; }

    // Navigation properties
    public Clinic Clinic { get; set; } = null!;
    public AppUser? User { get; set; }
}
