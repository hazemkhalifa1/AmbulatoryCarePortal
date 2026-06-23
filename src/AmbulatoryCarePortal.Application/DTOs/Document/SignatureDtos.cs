using AmbulatoryCarePortal.Domain.Enums;

namespace AmbulatoryCarePortal.Application.DTOs.Document;

public class ClinicSignatureDto
{
    public int Id { get; set; }
    public int ClinicId { get; set; }
    public string SignerCode { get; set; } = string.Empty;
    public string SignerName { get; set; } = string.Empty;
    public string SignerTitle { get; set; } = string.Empty;
    public string? SignatureImagePath { get; set; }
    public string SignatureType { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateClinicSignatureDto
{
    public string SignerCode { get; set; } = string.Empty;
    public string SignerName { get; set; } = string.Empty;
    public string SignerTitle { get; set; } = string.Empty;
    public string? SignatureImagePath { get; set; }
    public string SignatureType { get; set; } = "Drawn";
}

public class TemplateSignerDto
{
    public int Id { get; set; }
    public int DocumentTemplateId { get; set; }
    public string SignerCode { get; set; } = string.Empty;
    public string SignerDisplayName { get; set; } = string.Empty;
    public string SignerTitle { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
}

public class CreateTemplateSignerDto
{
    public string SignerCode { get; set; } = string.Empty;
    public string SignerDisplayName { get; set; } = string.Empty;
    public string SignerTitle { get; set; } = string.Empty;
    public bool IsRequired { get; set; } = true;
}

public class SignerViewModel
{
    public string SignerCode { get; set; } = string.Empty;
    public string SignerDisplayName { get; set; } = string.Empty;
    public string SignerTitle { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public bool HasSignature { get; set; }
    public string? SignatureImagePath { get; set; }
    public string? CurrentSignerName { get; set; }
    public string? CurrentSignerTitle { get; set; }
    public int? SignatureId { get; set; }
}
