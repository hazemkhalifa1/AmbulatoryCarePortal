using AmbulatoryCarePortal.Domain.Enums;

namespace AmbulatoryCarePortal.Application.DTOs.Document;

public class CreateDocumentTemplateDto
{
    public string StandardCode { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string? TitleAr { get; set; }
    public string? Description { get; set; }
    public string? DepartmentCategory { get; set; }
    public ClinicType ClinicType { get; set; }
}

public class UpdateDocumentTemplateDto
{
    public int Id { get; set; }
    public string StandardCode { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string? TitleAr { get; set; }
    public string? Description { get; set; }
    public string? DepartmentCategory { get; set; }
    public ClinicType ClinicType { get; set; }
    public bool IsActive { get; set; }
}

public class DocumentTemplateDto
{
    public int Id { get; set; }
    public string StandardCode { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string? TitleAr { get; set; }
    public string? Description { get; set; }
    public string? DepartmentCategory { get; set; }
    public ClinicType ClinicType { get; set; }
    public string? TemplateFilePath { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}


