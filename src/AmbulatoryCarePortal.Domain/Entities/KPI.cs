using AmbulatoryCarePortal.Domain.Enums;

namespace AmbulatoryCarePortal.Domain.Entities;

public class KPI : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public decimal TargetValue { get; set; }
    public KPIFrequency Frequency { get; set; } = KPIFrequency.Monthly;
    public int? DepartmentId { get; set; }
    public int ClinicId { get; set; }
    public string? CalculationFormula { get; set; }
    public string? EvidenceRequired { get; set; }
    public string? EscalationRule { get; set; }

    // Navigation properties
    public Clinic Clinic { get; set; } = null!;
    public Department? Department { get; set; }
    public ICollection<KPIEntry> MonthlyEntries { get; set; } = new List<KPIEntry>();
}
