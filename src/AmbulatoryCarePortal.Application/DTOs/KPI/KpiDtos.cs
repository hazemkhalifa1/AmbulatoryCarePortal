using AmbulatoryCarePortal.Domain.Enums;

namespace AmbulatoryCarePortal.Application.DTOs;

public class CreateKPIDto
{
    public string Name { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public decimal TargetValue { get; set; }
    public KPIFrequency Frequency { get; set; }
    public int? DepartmentId { get; set; }
    public int ClinicId { get; set; }
    public string? CalculationFormula { get; set; }
    public string? EvidenceRequired { get; set; }
}

public class KPIDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public decimal TargetValue { get; set; }
    public KPIFrequency Frequency { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal Achievement { get; set; }
}

public class KPIEntryDto
{
    public int Id { get; set; }
    public int KPIId { get; set; }
    public int PeriodMonth { get; set; }
    public int PeriodYear { get; set; }
    public decimal ActualValue { get; set; }
    public string? Notes { get; set; }
}
