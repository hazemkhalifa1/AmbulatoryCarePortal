using AmbulatoryCarePortal.Domain.Enums;

namespace AmbulatoryCarePortal.Domain.Entities;

public class PolicyDocument : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? TitleAr { get; set; }
    public string? StandardCode { get; set; }
    public int DepartmentId { get; set; }
    public int ClinicId { get; set; }
    public string? OfficialPdfPath { get; set; }
    public DocumentStatus DocumentStatus { get; set; } = DocumentStatus.NeedsReview;
    public DateTime? ExpiryDate { get; set; }
    public int VersionNumber { get; set; } = 1;

    // Navigation properties
    public Department Department { get; set; } = null!;
    public Clinic Clinic { get; set; } = null!;
    public ICollection<EvidenceAttachment> Attachments { get; set; } = new List<EvidenceAttachment>();
    public ICollection<AuditTrail> AuditTrails { get; set; } = new List<AuditTrail>();
}
