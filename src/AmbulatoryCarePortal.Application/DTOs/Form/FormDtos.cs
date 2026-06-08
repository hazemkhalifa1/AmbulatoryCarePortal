namespace AmbulatoryCarePortal.Application.DTOs;

public class FormDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? TitleAr { get; set; }
    public int VersionNumber { get; set; }
    public string? FilePath { get; set; }
    public int ClinicId { get; set; }
    public string? Category { get; set; }
    public bool IsActive { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateFormDto
{
    public string Title { get; set; } = string.Empty;
    public string? TitleAr { get; set; }
    public int ClinicId { get; set; }
    public string? Category { get; set; }
    public string? FilePath { get; set; }
}

public class FormVersionDto
{
    public int Id { get; set; }
    public int FormId { get; set; }
    public int VersionNumber { get; set; }
    public string? FilePath { get; set; }
    public string? UploadedByUserId { get; set; }
    public string? UploadedByUserName { get; set; }
    public DateTime UploadedAt { get; set; }
    public string? Notes { get; set; }
}
