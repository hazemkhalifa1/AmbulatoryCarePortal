using AmbulatoryCarePortal.Domain.Enums;

namespace AmbulatoryCarePortal.Domain.Entities;

public class DocumentTemplate : BaseEntity
{
    public string StandardCode { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string? TitleAr { get; set; }
    public string? Description { get; set; }
    public string? DepartmentCategory { get; set; }
    public ClinicType ClinicType { get; set; }
    public string? TemplateFilePath { get; set; }
    public bool IsActive { get; set; } = true;
    public int CurrentVersion { get; set; } = 1;

    public ICollection<ClinicDocument> ClinicDocuments { get; set; } = new List<ClinicDocument>();
    public ICollection<TemplateVariable> TemplateVariables { get; set; } = new List<TemplateVariable>();
    public ICollection<DocumentTemplateVersion> Versions { get; set; } = new List<DocumentTemplateVersion>();
    public ICollection<ClinicTemplateAssignment> ClinicAssignments { get; set; } = new List<ClinicTemplateAssignment>();
    public ICollection<GeneratedDocument> GeneratedDocuments { get; set; } = new List<GeneratedDocument>();
}
