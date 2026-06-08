using AmbulatoryCarePortal.Domain.Enums;

namespace AmbulatoryCarePortal.Application.DTOs;

public class CreateHrStaffDto
{
    public string FullNameEn { get; set; } = string.Empty;
    public string? FullNameAr { get; set; }
    public StaffType StaffType { get; set; }
    public int ClinicId { get; set; }
    public int? DepartmentId { get; set; }
    public string? NationalId { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
}

public class HrStaffDto
{
    public int Id { get; set; }
    public string FullNameEn { get; set; } = string.Empty;
    public string? FullNameAr { get; set; }
    public StaffType StaffType { get; set; }
    public string? DepartmentName { get; set; }
    public bool IsActive { get; set; }
    public int DocumentCount { get; set; }
}

public class HrStaffDetailDto
{
    public int Id { get; set; }
    public string FullNameEn { get; set; } = string.Empty;
    public string? FullNameAr { get; set; }
    public StaffType StaffType { get; set; }
    public string? DepartmentName { get; set; }
    public string? NationalId { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; }
    public List<HrDocumentDto> Documents { get; set; } = new();
}

public class HrDocumentDto
{
    public int Id { get; set; }
    public string DocumentName { get; set; } = string.Empty;
    public HrDocumentType DocumentType { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool IsVerified { get; set; }
    public int DaysUntilExpiry { get; set; }
}

public class CreateHrDocumentDto
{
    public int HrStaffId { get; set; }
    public string DocumentName { get; set; } = string.Empty;
    public string? DocumentNameAr { get; set; }
    public HrDocumentType DocumentType { get; set; }
    public DateTime? ExpiryDate { get; set; }
}
