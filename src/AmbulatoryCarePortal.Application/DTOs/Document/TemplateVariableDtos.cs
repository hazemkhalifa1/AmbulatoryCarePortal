namespace AmbulatoryCarePortal.Application.DTOs.Document;

public class TemplateVariableDto
{
    public int Id { get; set; }
    public int DocumentTemplateId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsImage { get; set; }
    public bool IsRequired { get; set; }
    public string? DefaultValue { get; set; }
}

public class CreateTemplateVariableDto
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsImage { get; set; }
    public bool IsRequired { get; set; }
    public string? DefaultValue { get; set; }
}

public class UpdateTemplateVariableDto
{
    public int Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public bool IsImage { get; set; }
    public bool IsRequired { get; set; }
    public string? DefaultValue { get; set; }
}

public class ClinicTemplateAssignmentDto
{
    public int Id { get; set; }
    public int ClinicId { get; set; }
    public string ClinicName { get; set; } = string.Empty;
    public int DocumentTemplateId { get; set; }
    public string StandardCode { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string? TitleAr { get; set; }
    public string AssignmentStatus { get; set; } = string.Empty;
    public DateTime? ExpiryDate { get; set; }
    public string? Notes { get; set; }
    public int ValueCount { get; set; }
    public int GeneratedDocumentCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ClinicTemplateValueDto
{
    public int Id { get; set; }
    public int ClinicTemplateAssignmentId { get; set; }
    public int TemplateVariableId { get; set; }
    public string VariableName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsImage { get; set; }
    public bool IsRequired { get; set; }
    public bool IsSignature { get; set; }
    public string? Value { get; set; }
    public string? ImagePath { get; set; }
}

public class UpsertClinicTemplateValueDto
{
    public int TemplateVariableId { get; set; }
    public string? Value { get; set; }
}

public class GeneratedDocumentDto
{
    public int Id { get; set; }
    public int ClinicTemplateAssignmentId { get; set; }
    public int DocumentTemplateId { get; set; }
    public int ClinicId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string? GeneratedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class DocumentTemplateVersionDto
{
    public int Id { get; set; }
    public int DocumentTemplateId { get; set; }
    public int VersionNumber { get; set; }
    public string? FilePath { get; set; }
    public string? ChangeLog { get; set; }
    public string? UploadedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class TemplateVariablePreviewDto
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsImage { get; set; }
    public bool IsRequired { get; set; }
    public bool HasValue { get; set; }
    public string? CurrentValue { get; set; }
}

public class ClinicAssignmentDetailDto
{
    public int AssignmentId { get; set; }
    public int DocumentTemplateId { get; set; }
    public string StandardCode { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string? TitleAr { get; set; }
    public string AssignmentStatus { get; set; } = string.Empty;
    public string? TemplateFilePath { get; set; }
    public bool HasTemplateFile => !string.IsNullOrEmpty(TemplateFilePath);
    public List<ClinicTemplateValueDto> VariableValues { get; set; } = new();
}

public class GlobalTemplateValueDto
{
    public string VariableName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsImage { get; set; }
    public bool IsRequired { get; set; }
    public bool IsSignature { get; set; }
    public string? Value { get; set; }
    public string? ImagePath { get; set; }
    public bool IsAutoPopulated { get; set; }
}

public class UpsertGlobalTemplateValueDto
{
    public string VariableName { get; set; } = string.Empty;
    public string? Value { get; set; }
}

public class TemplateDetailsDto
{
    public int Id { get; set; }
    public string StandardCode { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string? TitleAr { get; set; }
    public string? Description { get; set; }
    public string? DepartmentCategory { get; set; }
    public string ClinicType { get; set; } = string.Empty;
    public string? TemplateFilePath { get; set; }
    public bool IsActive { get; set; }
    public int CurrentVersion { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<TemplateVariableDto> Variables { get; set; } = new();
    public List<DocumentTemplateVersionDto> Versions { get; set; } = new();
    public List<ClinicTemplateAssignmentDto> Assignments { get; set; } = new();
    public List<GeneratedDocumentDto> GeneratedDocuments { get; set; } = new();
}
