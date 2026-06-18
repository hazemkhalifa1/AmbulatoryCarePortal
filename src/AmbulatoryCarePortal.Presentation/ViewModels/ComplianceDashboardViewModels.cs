using AmbulatoryCarePortal.Application.DTOs;
using AmbulatoryCarePortal.Application.DTOs.Analytics;

namespace AmbulatoryCarePortal.Presentation.ViewModels;

public class ComplianceDashboardViewModel
{
    public decimal OverallScore { get; set; }
    public List<ScoreComponentDto> Components { get; set; } = [];
    public List<ScoreTrendDto> ScoreTrend { get; set; } = [];
    public int MissingPolicies { get; set; }
    public int ExpiredDocuments { get; set; }
    public int OverdueChecklists { get; set; }
    public int ExpiringCredentials { get; set; }
    public int OpenCapaCount { get; set; }
    public int OverdueCapaCount { get; set; }
    public List<ExpiryItemDto> UpcomingExpiries { get; set; } = [];
    public List<CapaItemDto> OpenCapas { get; set; } = [];
    public List<DepartmentScoreDto> DepartmentPerformance { get; set; } = [];
    public List<ComplianceCalendarItemDto> CalendarItems { get; set; } = [];

    public string ScoreColor => OverallScore switch { >= 80 => "success", >= 60 => "warning", _ => "danger" };
    public string ScoreStatus => OverallScore switch { >= 80 => "Compliant", >= 60 => "Partial", _ => "NonCompliant" };
}

public class ExpiryItemDto
{
    public string Title { get; set; } = string.Empty;
    public string? TitleAr { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? Department { get; set; }
    public DateTime ExpiryDate { get; set; }
    public int DaysRemaining { get; set; }
    public string? DetailUrl { get; set; }
}

public class CapaItemDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? TitleAr { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public DateTime? TargetDate { get; set; }
    public DateTime? CreatedAt { get; set; }
    public string? AssignedTo { get; set; }
    public string? Department { get; set; }
}

public class DepartmentScoreDto
{
    public string Name { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public decimal Score { get; set; }
    public int StaffCount { get; set; }
    public int CompletedItems { get; set; }
    public int TotalItems { get; set; }
}
