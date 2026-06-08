namespace AmbulatoryCarePortal.Domain.Entities;

public class Form : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? TitleAr { get; set; }
    public int VersionNumber { get; set; } = 1;
    public string? FilePath { get; set; }
    public int ClinicId { get; set; }
    public string? Category { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? PublishedAt { get; set; }

    // Navigation properties
    public Clinic Clinic { get; set; } = null!;
    public ICollection<FormVersion> VersionHistory { get; set; } = new List<FormVersion>();
}
