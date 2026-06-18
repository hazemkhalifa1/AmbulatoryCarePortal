using AmbulatoryCarePortal.Domain.Enums;

namespace AmbulatoryCarePortal.Domain.Entities;

public class GeneratedDocument : BaseEntity
{
    public int ClinicTemplateAssignmentId { get; set; }
    public int DocumentTemplateId { get; set; }
    public int ClinicId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string? GeneratedByUserId { get; set; }

    public ClinicTemplateAssignment ClinicTemplateAssignment { get; set; } = null!;
    public DocumentTemplate DocumentTemplate { get; set; } = null!;
    public Clinic Clinic { get; set; } = null!;
}
