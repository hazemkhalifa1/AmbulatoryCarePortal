using AmbulatoryCarePortal.Application.DTOs.Analytics;

namespace AmbulatoryCarePortal.Application.Interfaces;

public interface IAnalyticsService
{
    Task<ComplianceOverviewDto> GetComplianceAnalyticsAsync(int clinicId);
    Task<List<InsightDto>> GetComplianceInsightsAsync(int clinicId);
    Task<List<TrendDto>> GetComplianceTrendsAsync(int clinicId, int months = 6);
    Task<Dictionary<string, object>> GetDashboardMetricsAsync(int clinicId, string userRole);
}
