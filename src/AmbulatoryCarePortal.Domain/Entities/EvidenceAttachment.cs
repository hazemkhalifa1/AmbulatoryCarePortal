namespace AmbulatoryCarePortal.Domain.Entities;

public class EvidenceAttachment : BaseEntity
{
    public string DocumentName { get; set; } = string.Empty;
    public string? DocumentNameAr { get; set; }
    public string? FilePath { get; set; }
    public string? FileType { get; set; }
    public int PolicyDocumentId { get; set; }
    public string? UploadedByUserId { get; set; }
    public DateTime UploadDate { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiryDate { get; set; }
    public int VersionNumber { get; set; } = 1;
    public string? Notes { get; set; }

    // Navigation properties
    public PolicyDocument PolicyDocument { get; set; } = null!;
    public AppUser? UploadedByUser { get; set; }
}
