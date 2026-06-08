namespace AmbulatoryCarePortal.Application.DTOs.Analytics;

public class ComplianceOverviewDto
{
    public int ClinicId { get; set; }
    public string ClinicName { get; set; } = string.Empty;
    public decimal OverallScore { get; set; }
    public int TotalPolicies { get; set; }
    public int ApprovedPolicies { get; set; }
    public int PendingPolicies { get; set; }
    public decimal PolicyCompletionRate { get; set; }
    public int TotalChecklists { get; set; }
    public int CompletedChecklists { get; set; }
    public decimal ChecklistCompletionRate { get; set; }
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
}

public class InsightDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = "Info";
    public string Severity { get; set; } = "Info";
    public string Category { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
}

public class TrendDto
{
    public string Metric { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public decimal ChangePercentage { get; set; }
    public string Direction { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string MetricName { get; set; } = string.Empty;
}

public class NotificationSettingDto
{
    public string NotificationType { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public int DaysBeforeAlert { get; set; }
    public bool EmailEnabled { get; set; }
    public bool InAppEnabled { get; set; }
}
