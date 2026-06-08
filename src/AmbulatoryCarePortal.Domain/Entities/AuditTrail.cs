using AmbulatoryCarePortal.Domain.Enums;

namespace AmbulatoryCarePortal.Domain.Entities;

public class AuditTrail : BaseEntity
{
    public int ClinicId { get; set; }
    public AuditActionType ActionType { get; set; }
    public int? TargetObjectId { get; set; }
    public string TargetObjectType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public DateTime ActionDate { get; set; } = DateTime.UtcNow;
    public string? UserId { get; set; }
    public string? IpAddress { get; set; }

    // Navigation properties
    public Clinic Clinic { get; set; } = null!;
    public AppUser? User { get; set; }
}
