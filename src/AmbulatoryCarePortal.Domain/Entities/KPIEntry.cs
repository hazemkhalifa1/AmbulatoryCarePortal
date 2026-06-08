namespace AmbulatoryCarePortal.Domain.Entities;

public class KPIEntry : BaseEntity
{
    public int KPIId { get; set; }
    public int PeriodMonth { get; set; }
    public int PeriodYear { get; set; }
    public decimal ActualValue { get; set; }
    public string? Notes { get; set; }
    public string? EvidenceFilePath { get; set; }

    // Navigation properties
    public KPI KPI { get; set; } = null!;
}
