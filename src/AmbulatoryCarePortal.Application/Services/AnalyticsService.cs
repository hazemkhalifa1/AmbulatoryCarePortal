using AmbulatoryCarePortal.Application.DTOs.Analytics;
using AmbulatoryCarePortal.Application.Interfaces;
using AmbulatoryCarePortal.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace AmbulatoryCarePortal.Application.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AnalyticsService> _logger;

    public AnalyticsService(IUnitOfWork unitOfWork, ILogger<AnalyticsService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ComplianceOverviewDto> GetComplianceAnalyticsAsync(int clinicId)
    {
        var clinic = await _unitOfWork.Repository<Clinic>().GetByIdAsync(clinicId);
        var checklists = await _unitOfWork.Repository<ChecklistRound>().FindAsync(c => c.ClinicId == clinicId);

        return new ComplianceOverviewDto
        {
            OverallScore = clinic?.ComplianceScore ?? 0,
            TotalPolicies = 0,
            ApprovedPolicies = 0,
            PendingPolicies = 0,
            PolicyCompletionRate = 0,
            TotalChecklists = checklists.Count(),
            CompletedChecklists = checklists.Count(c => c.ExecutedAt != default),
            ChecklistCompletionRate = checklists.Any() ? (checklists.Count(c => c.ExecutedAt != default) * 100m / checklists.Count()) : 0
        };
    }

    public async Task<List<InsightDto>> GetComplianceInsightsAsync(int clinicId)
    {
        var insights = new List<InsightDto>();
        var overview = await GetComplianceAnalyticsAsync(clinicId);

        if (overview.OverallScore < 60)
        {
            insights.Add(new InsightDto
            {
                Title = "Low Compliance Score",
                Description = $"Current compliance score is {overview.OverallScore}%",
                Type = "Danger",
                Recommendation = "Implement immediate corrective actions"
            });
        }

        if (overview.PolicyCompletionRate < 80)
        {
            insights.Add(new InsightDto
            {
                Title = "Incomplete Policies",
                Description = $"Only {overview.PolicyCompletionRate}% of policies are approved",
                Type = "Warning",
                Recommendation = "Expedite policy approval process"
            });
        }

        if (overview.ChecklistCompletionRate < 70)
        {
            insights.Add(new InsightDto
            {
                Title = "Low Checklist Execution",
                Description = $"Only {overview.ChecklistCompletionRate}% of checklists are completed",
                Type = "Warning",
                Recommendation = "Increase checklist monitoring and follow-up"
            });
        }

        return await Task.FromResult(insights);
    }

    public async Task<List<TrendDto>> GetComplianceTrendsAsync(int clinicId, int months = 6)
    {
        var trends = new List<TrendDto>
        {
            new TrendDto
            {
                Metric = "Compliance Score",
                ChangePercentage = 5.5m,
                Direction = "Up",
                Period = "Last 6 months"
            },
            new TrendDto
            {
                Metric = "Policy Approval Rate",
                ChangePercentage = 3.2m,
                Direction = "Up",
                Period = "Last 6 months"
            }
        };

        return await Task.FromResult(trends);
    }

    public async Task<Dictionary<string, object>> GetDashboardMetricsAsync(int clinicId, string userRole)
    {
        var metrics = new Dictionary<string, object>();

        var clinic = await _unitOfWork.Repository<Clinic>().GetByIdAsync(clinicId);
        var checklists = await _unitOfWork.Repository<ChecklistRound>().FindAsync(c => c.ClinicId == clinicId);

        metrics["TotalClinics"] = 1;
        metrics["AverageCompliance"] = clinic?.ComplianceScore ?? 0;
        metrics["PendingApprovals"] = 0;
        metrics["OverdueItems"] = 0;
        metrics["ExpiringDocuments"] = 0;

        return await Task.FromResult(metrics);
    }
}
