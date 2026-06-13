namespace AmbulatoryCarePortal.Domain.Entities;

public class ComplianceScoreSnapshot : BaseEntity
{
    public int ClinicId { get; set; }
    public decimal OverallScore { get; set; }
    public decimal PolicyScore { get; set; }
    public decimal KpiScore { get; set; }
    public decimal ChecklistScore { get; set; }
    public decimal HrScore { get; set; }
    public decimal DocumentScore { get; set; }
    public decimal PolicyWeight { get; set; } = 25m;
    public decimal KpiWeight { get; set; } = 20m;
    public decimal ChecklistWeight { get; set; } = 25m;
    public decimal HrWeight { get; set; } = 20m;
    public decimal DocumentWeight { get; set; } = 10m;
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;

    public Clinic Clinic { get; set; } = null!;
}
