using AmbulatoryCarePortal.Application.DTOs.Analytics;

namespace AmbulatoryCarePortal.Application.Interfaces;

public interface IComplianceScoreService
{
    Task<ComplianceScoreDto> CalculateScoreAsync(int clinicId);
    Task<ComplianceScoreDto> GetLatestScoreAsync(int clinicId);
    Task<List<ScoreTrendDto>> GetScoreTrendAsync(int clinicId, int days = 90);
    Task<List<ComplianceScoreSnapshotDto>> GetScoreHistoryAsync(int clinicId, int count = 10);
    Task<ComplianceDashboardDto> GetDashboardAsync(int clinicId);
    Task<List<ComplianceScoreDto>> GetAllClinicsScoresAsync();
}
