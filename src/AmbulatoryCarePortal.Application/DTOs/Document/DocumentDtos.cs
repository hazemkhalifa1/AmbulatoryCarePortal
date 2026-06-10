using AmbulatoryCarePortal.Domain.Enums;

namespace AmbulatoryCarePortal.Application.DTOs.Document;

public class CreateDocumentTemplateDto
{
    public string StandardCode { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string? TitleAr { get; set; }
    public string? Description { get; set; }
    public string? DepartmentCategory { get; set; }
}

public class UpdateDocumentTemplateDto
{
    public int Id { get; set; }
    public string StandardCode { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string? TitleAr { get; set; }
    public string? Description { get; set; }
    public string? DepartmentCategory { get; set; }
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
    public string? TemplateFilePath { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ClinicDocumentDto
{
    public int Id { get; set; }
    public int ClinicId { get; set; }
    public int DocumentTemplateId { get; set; }
    public string StandardCode { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string? TitleAr { get; set; }
    public string? DepartmentCategory { get; set; }
    public ClinicDocumentStatus DocumentStatus { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? OfficialPdfPath { get; set; }
    public string? Notes { get; set; }
    public int AttachmentCount { get; set; }
}

public class ClinicDocumentDetailDto
{
    public int Id { get; set; }
    public int ClinicId { get; set; }
    public int DocumentTemplateId { get; set; }
    public string StandardCode { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string? TitleAr { get; set; }
    public string? DepartmentCategory { get; set; }
    public ClinicDocumentStatus DocumentStatus { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? OfficialPdfPath { get; set; }
    public string? Notes { get; set; }
    public List<ClinicDocumentAttachmentDto> Attachments { get; set; } = new();
}

public class ClinicDocumentAttachmentDto
{
    public int Id { get; set; }
    public int ClinicDocumentId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string? FileType { get; set; }
    public DateTime UploadedAt { get; set; }
    public string? Notes { get; set; }
}
