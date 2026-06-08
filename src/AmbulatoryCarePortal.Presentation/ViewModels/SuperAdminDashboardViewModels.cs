using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AmbulatoryCarePortal.Application.Interfaces;

namespace AmbulatoryCarePortal.Presentation.ViewModels;

public class CreateClinicViewModel
{
    public string Name { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public string? CityEn { get; set; }
    public string? CityAr { get; set; }
    public AmbulatoryCarePortal.Domain.Enums.ClinicType ClinicType { get; set; }
    public string? LicenseNumber { get; set; }
    public DateTime? LicenseExpiry { get; set; }
}
