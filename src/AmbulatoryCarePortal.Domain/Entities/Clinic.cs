using AmbulatoryCarePortal.Domain.Enums;

namespace AmbulatoryCarePortal.Domain.Entities;

public class Clinic : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public string? CityEn { get; set; }
    public string? CityAr { get; set; }
    public ClinicType ClinicType { get; set; }
    public string? LogoPath { get; set; }
    public string? LicenseNumber { get; set; }
    public DateTime? LicenseExpiry { get; set; }
    public bool IsActive { get; set; } = true;
    public decimal ComplianceScore { get; set; } = 0;

    // Navigation properties
    public ICollection<AppUser> Users { get; set; } = new List<AppUser>();
    public ICollection<Department> Departments { get; set; } = new List<Department>();
    public ICollection<PolicyDocument> PolicyDocuments { get; set; } = new List<PolicyDocument>();
    public ICollection<KPI> KPIs { get; set; } = new List<KPI>();
    public ICollection<ChecklistTemplate> ChecklistTemplates { get; set; } = new List<ChecklistTemplate>();
    public ICollection<ChecklistRound> ChecklistRounds { get; set; } = new List<ChecklistRound>();
    public ICollection<Form> Forms { get; set; } = new List<Form>();
    public ICollection<HrStaff> HrStaff { get; set; } = new List<HrStaff>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<AuditTrail> AuditTrails { get; set; } = new List<AuditTrail>();
    public ICollection<ClinicDocument> ClinicDocuments { get; set; } = new List<ClinicDocument>();
    public ICollection<ComplianceScoreSnapshot> ComplianceScoreSnapshots { get; set; } = new List<ComplianceScoreSnapshot>();
}
