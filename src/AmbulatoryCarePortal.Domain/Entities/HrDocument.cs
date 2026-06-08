using AmbulatoryCarePortal.Domain.Enums;

namespace AmbulatoryCarePortal.Domain.Entities;

public class HrDocument : BaseEntity
{
    public int HrStaffId { get; set; }
    public string DocumentName { get; set; } = string.Empty;
    public string? DocumentNameAr { get; set; }
    public HrDocumentType DocumentType { get; set; }
    public string? FilePath { get; set; }
    public DateTime? IssueDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? UploadedByUserId { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public bool IsVerified { get; set; } = false;
    public string? VerifiedByUserId { get; set; }
    public DateTime? VerifiedDate { get; set; }
    public int VersionNumber { get; set; } = 1;

    // Navigation properties
    public HrStaff HrStaff { get; set; } = null!;
    public AppUser? UploadedByUser { get; set; }
}
