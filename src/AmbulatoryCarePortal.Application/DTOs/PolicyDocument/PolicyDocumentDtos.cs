using AmbulatoryCarePortal.Domain.Enums;

namespace AmbulatoryCarePortal.Application.DTOs.PolicyDocument;

public class CreatePolicyDocumentDto
{
    public string Title { get; set; } = string.Empty;
    public string? TitleAr { get; set; }
    public string? StandardCode { get; set; }
    public int DepartmentId { get; set; }
    public int ClinicId { get; set; }
    public DateTime? ExpiryDate { get; set; }
}

public class UpdatePolicyDocumentDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? TitleAr { get; set; }
    public DocumentStatus DocumentStatus { get; set; }
    public DateTime? ExpiryDate { get; set; }
}

public class PolicyDocumentDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? TitleAr { get; set; }
    public string? StandardCode { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public DocumentStatus DocumentStatus { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public int VersionNumber { get; set; }
    public int AttachmentCount { get; set; }
}

public class PolicyDocumentDetailDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? TitleAr { get; set; }
    public string? StandardCode { get; set; }
    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public int ClinicId { get; set; }
    public string? OfficialPdfPath { get; set; }
    public DocumentStatus DocumentStatus { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public int VersionNumber { get; set; }
    public List<EvidenceAttachmentDto> Attachments { get; set; } = new();
}

public class EvidenceAttachmentDto
{
    public int Id { get; set; }
    public string DocumentName { get; set; } = string.Empty;
    public string? DocumentNameAr { get; set; }
    public string? FileType { get; set; }
    public DateTime UploadDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public int VersionNumber { get; set; }
    public string? Notes { get; set; }
}
