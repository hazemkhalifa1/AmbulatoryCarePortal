using AmbulatoryCarePortal.Domain.Enums;

namespace AmbulatoryCarePortal.Domain.Entities;

public class Department : BaseEntity
{
    public string NameEn { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public DepartmentCodeEnum DepartmentCode { get; set; }
    public int ClinicId { get; set; }

    // Navigation properties
    public Clinic Clinic { get; set; } = null!;
    public ICollection<AppUser> Users { get; set; } = new List<AppUser>();
    public ICollection<PolicyDocument> PolicyDocuments { get; set; } = new List<PolicyDocument>();
    public ICollection<HrStaff> Staff { get; set; } = new List<HrStaff>();
    public ICollection<KPI> KPIs { get; set; } = new List<KPI>();
    public ICollection<ChecklistTemplate> ChecklistTemplates { get; set; } = new List<ChecklistTemplate>();
    public ICollection<ChecklistRound> ChecklistRounds { get; set; } = new List<ChecklistRound>();
}
