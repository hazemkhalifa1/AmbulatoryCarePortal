namespace AmbulatoryCarePortal.Domain.Entities;

public class FormVersion : BaseEntity
{
    public int FormId { get; set; }
    public int VersionNumber { get; set; }
    public string? FilePath { get; set; }
    public string? UploadedByUserId { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }

    // Navigation properties
    public Form Form { get; set; } = null!;
    public AppUser? UploadedByUser { get; set; }
}
