using AmbulatoryCarePortal.Application.DTOs.Analytics;

namespace AmbulatoryCarePortal.Presentation.ViewModels;

public class ComplianceScoreWidgetViewModel
{
    public ComplianceScoreDto Score { get; set; } = new();
    public ComplianceDashboardDto Dashboard { get; set; } = new();
    public string ScoreColor => Score.OverallScore switch
    {
        >= 80 => "text-success",
        >= 60 => "text-warning",
        _ => "text-danger"
    };
    public string ScoreBgColor => Score.OverallScore switch
    {
        >= 80 => "bg-success",
        >= 60 => "bg-warning",
        _ => "bg-danger"
    };
}

public class ScoreChartDataViewModel
{
    public List<string> Labels { get; set; } = [];
    public List<decimal> Scores { get; set; } = [];
    public List<decimal> WeightedScores { get; set; } = [];
    public List<string> Colors { get; set; } = [];
}

public class ScoreTrendChartViewModel
{
    public List<string> Labels { get; set; } = [];
    public List<decimal> Data { get; set; } = [];
}
