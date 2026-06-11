using Microsoft.AspNetCore.Http;
using AmbulatoryCarePortal.Application.DTOs.Clinic;

namespace AmbulatoryCarePortal.Presentation.ViewModels;

public class SuperAdminDashboardViewModel
{
    public DashboardMetricsViewModel Metrics { get; set; } = new();
    public List<ClinicDto> RecentClinics { get; set; } = new();
}

public class CreateClinicViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public string? CityEn { get; set; }
    public string? CityAr { get; set; }
    public AmbulatoryCarePortal.Domain.Enums.ClinicType ClinicType { get; set; }
    public string? LicenseNumber { get; set; }
    public DateTime? LicenseExpiry { get; set; }
    public bool IsActive { get; set; } = true;
    public IFormFile? LogoFile { get; set; }
    public string? ExistingLogoPath { get; set; }
}
