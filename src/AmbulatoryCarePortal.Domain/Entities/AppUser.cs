using Microsoft.AspNetCore.Identity;

namespace AmbulatoryCarePortal.Domain.Entities;

public class AppUser : IdentityUser
{
    public string? FullNameEn { get; set; }
    public string? FullNameAr { get; set; }
    public int? ClinicId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public string? ProfilePhotoPath { get; set; }

    // Navigation properties
    public Clinic? Clinic { get; set; }
    public ICollection<AuditTrail> AuditTrails { get; set; } = new List<AuditTrail>();
    public ICollection<ChecklistRound> ChecklistRounds { get; set; } = new List<ChecklistRound>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<HrDocument> HrDocuments { get; set; } = new List<HrDocument>();
}
