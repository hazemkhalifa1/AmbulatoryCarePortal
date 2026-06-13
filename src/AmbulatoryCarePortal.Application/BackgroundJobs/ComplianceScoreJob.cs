using AmbulatoryCarePortal.Application.Interfaces;
using AmbulatoryCarePortal.Domain.Entities;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace AmbulatoryCarePortal.Application.BackgroundJobs;

public class ComplianceScoreJob
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IComplianceScoreService _scoreService;
    private readonly ILogger<ComplianceScoreJob> _logger;

    public ComplianceScoreJob(
        IUnitOfWork unitOfWork,
        IComplianceScoreService scoreService,
        ILogger<ComplianceScoreJob> logger)
    {
        _unitOfWork = unitOfWork;
        _scoreService = scoreService;
        _logger = logger;
    }

    [DisableConcurrentExecution(300)]
    [AutomaticRetry(Attempts = 2, DelaysInSeconds = [120, 600])]
    public async Task RunAsync(CancellationToken ct)
    {
        _logger.LogInformation("Compliance score calculation starting for all clinics");

        var clinics = await _unitOfWork.Repository<Clinic>().FindAsync(c => c.IsActive);
        foreach (var clinic in clinics)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                await _scoreService.CalculateScoreAsync(clinic.Id);
                _logger.LogInformation("Score calculated for clinic {ClinicId}", clinic.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to calculate score for clinic {ClinicId}", clinic.Id);
            }
        }

        _logger.LogInformation("Compliance score calculation completed for {Count} clinics", clinics.Count());
    }
}
