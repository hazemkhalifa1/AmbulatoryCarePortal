using AmbulatoryCarePortal.Domain.Enums;

namespace AmbulatoryCarePortal.Application.DTOs;

public class DepartmentDto
{
    public int Id { get; set; }
    public string NameEn { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public DepartmentCodeEnum DepartmentCode { get; set; }
    public int ClinicId { get; set; }
    public string? ClinicName { get; set; }
}

public class CreateDepartmentDto
{
    public string NameEn { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public DepartmentCodeEnum DepartmentCode { get; set; }
    public int ClinicId { get; set; }
}
