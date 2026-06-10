namespace AmbulatoryCarePortal.Domain.Entities;

public class ClinicDocumentAttachment : BaseEntity
{
    public int ClinicDocumentId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string? FilePath { get; set; }
    public string? FileType { get; set; }
    public string? UploadedByUserId { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }

    // Navigation properties
    public ClinicDocument ClinicDocument { get; set; } = null!;
    public AppUser? UploadedByUser { get; set; }
}
