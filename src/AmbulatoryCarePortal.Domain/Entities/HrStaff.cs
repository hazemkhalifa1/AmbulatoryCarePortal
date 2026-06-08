using AmbulatoryCarePortal.Domain.Enums;

namespace AmbulatoryCarePortal.Domain.Entities;

public class HrStaff : BaseEntity
{
    public string FullNameEn { get; set; } = string.Empty;
    public string? FullNameAr { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public StaffType StaffType { get; set; }
    public int ClinicId { get; set; }
    public int? DepartmentId { get; set; }
    public string? NationalId { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? PhoneNumber { get; set; }
    public string? PositionTitle { get; set; }
    public DateTime? JoinDate { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public Clinic Clinic { get; set; } = null!;
    public Department? Department { get; set; }
    public ICollection<HrDocument> Documents { get; set; } = new List<HrDocument>();
}
