using AmbulatoryCarePortal.Domain.Enums;

namespace AmbulatoryCarePortal.Domain.Entities;

public class ChecklistTemplate : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public string? Description { get; set; }
    public int ClinicId { get; set; }
    public int? DepartmentId { get; set; }
    public ChecklistSchedule Frequency { get; set; } = ChecklistSchedule.Monthly;
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public Clinic Clinic { get; set; } = null!;
    public Department? Department { get; set; }
    public ICollection<ChecklistItem> Items { get; set; } = new List<ChecklistItem>();
    public ICollection<ChecklistRound> Rounds { get; set; } = new List<ChecklistRound>();
}
