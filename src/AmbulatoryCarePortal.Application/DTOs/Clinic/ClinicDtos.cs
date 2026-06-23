using AmbulatoryCarePortal.Application.DTOs.Document;
using AmbulatoryCarePortal.Domain.Enums;

namespace AmbulatoryCarePortal.Application.DTOs.Clinic;

public class CreateClinicDto
{
    public string Name { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public string? CityEn { get; set; }
    public string? CityAr { get; set; }
    public ClinicType ClinicType { get; set; }
    public string? LicenseNumber { get; set; }
    public DateTime? LicenseExpiry { get; set; }
}

public class UpdateClinicDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public string? CityEn { get; set; }
    public string? CityAr { get; set; }
    public ClinicType ClinicType { get; set; }
    public string? LicenseNumber { get; set; }
    public DateTime? LicenseExpiry { get; set; }
    public bool IsActive { get; set; }
}

public class ClinicDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public string? CityEn { get; set; }
    public string? CityAr { get; set; }
    public ClinicType ClinicType { get; set; }
    public string? LogoPath { get; set; }
    public string? LicenseNumber { get; set; }
    public DateTime? LicenseExpiry { get; set; }
    public bool IsActive { get; set; }
    public decimal ComplianceScore { get; set; }
}

public class ClinicDetailDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public string? CityEn { get; set; }
    public string? CityAr { get; set; }
    public ClinicType ClinicType { get; set; }
    public string? LogoPath { get; set; }
    public string? LicenseNumber { get; set; }
    public DateTime? LicenseExpiry { get; set; }
    public bool IsActive { get; set; }
    public decimal ComplianceScore { get; set; }
    public int UserCount { get; set; }
    public int DepartmentCount { get; set; }
    public int PolicyDocumentCount { get; set; }
    public int OpenGapCount { get; set; }
    public List<ClinicAssignmentDetailDto> DocumentAssignments { get; set; } = new();
    public List<GlobalTemplateValueDto> GlobalTemplateValues { get; set; } = new();
}
