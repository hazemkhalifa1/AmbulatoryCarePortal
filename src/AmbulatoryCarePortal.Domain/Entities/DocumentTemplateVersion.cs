namespace AmbulatoryCarePortal.Domain.Entities;

public class DocumentTemplateVersion : BaseEntity
{
    public int DocumentTemplateId { get; set; }
    public int VersionNumber { get; set; }
    public string? FilePath { get; set; }
    public string? ChangeLog { get; set; }
    public string? UploadedByUserId { get; set; }

    public DocumentTemplate DocumentTemplate { get; set; } = null!;
}
