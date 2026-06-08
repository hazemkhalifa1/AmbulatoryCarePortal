namespace AmbulatoryCarePortal.Domain.Entities;

public class ChecklistRound : BaseEntity
{
    public int ChecklistTemplateId { get; set; }
    public int ClinicId { get; set; }
    public int? DepartmentId { get; set; }
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
    public string? ExecutedByUserId { get; set; }
    public string? Notes { get; set; }
    public string? EvidenceFilePath { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovedByUserId { get; set; }

    // Navigation properties
    public ChecklistTemplate ChecklistTemplate { get; set; } = null!;
    public Clinic Clinic { get; set; } = null!;
    public Department? Department { get; set; }
    public AppUser? ExecutedByUser { get; set; }
    public AppUser? ApprovedByUser { get; set; }
    public ICollection<ChecklistAnswer> Answers { get; set; } = new List<ChecklistAnswer>();
}
