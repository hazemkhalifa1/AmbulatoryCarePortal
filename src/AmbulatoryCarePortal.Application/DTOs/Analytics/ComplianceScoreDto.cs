namespace AmbulatoryCarePortal.Application.DTOs.Analytics;

public class ComplianceScoreDto
{
    public int ClinicId { get; set; }
    public string ClinicName { get; set; } = string.Empty;
    public decimal OverallScore { get; set; }
    public List<ScoreComponentDto> Components { get; set; } = [];
    public DateTime CalculatedAt { get; set; }
}

public class ScoreComponentDto
{
    public string Name { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public decimal Score { get; set; }
    public decimal Weight { get; set; }
    public decimal WeightedScore => Score * Weight / 100m;
    public string Color { get; set; } = "#6c757d";
    public string Icon { get; set; } = string.Empty;
}

public class ScoreTrendDto
{
    public DateTime Date { get; set; }
    public decimal Score { get; set; }
}

public class ComplianceScoreSnapshotDto
{
    public int Id { get; set; }
    public int ClinicId { get; set; }
    public decimal OverallScore { get; set; }
    public decimal PolicyScore { get; set; }
    public decimal KpiScore { get; set; }
    public decimal ChecklistScore { get; set; }
    public decimal HrScore { get; set; }
    public decimal DocumentScore { get; set; }
    public DateTime CalculatedAt { get; set; }
}

public class ComplianceDashboardDto
{
    public ComplianceScoreDto CurrentScore { get; set; } = new();
    public List<ScoreTrendDto> Trend { get; set; } = [];
    public int MissingPolicies { get; set; }
    public int ExpiredDocuments { get; set; }
    public int OverdueChecklists { get; set; }
    public int ExpiringCredentials { get; set; }
}
