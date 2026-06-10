namespace AmbulatoryCarePortal.Domain.Entities;

public class DocumentTemplate : BaseEntity
{
    public string StandardCode { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string? TitleAr { get; set; }
    public string? Description { get; set; }
    public string? DepartmentCategory { get; set; }
    public string? TemplateFilePath { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<ClinicDocument> ClinicDocuments { get; set; } = new List<ClinicDocument>();
}
