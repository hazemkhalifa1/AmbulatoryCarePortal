using AmbulatoryCarePortal.Application.Interfaces;
using AmbulatoryCarePortal.Domain.Entities;
using AmbulatoryCarePortal.Domain.Enums;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace AmbulatoryCarePortal.Application.BackgroundJobs;

public class WeeklyDigestJob
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<WeeklyDigestJob> _logger;

    public WeeklyDigestJob(
        IUnitOfWork unitOfWork,
        ISettingsService settingsService,
        ILogger<WeeklyDigestJob> logger)
    {
        _unitOfWork = unitOfWork;
        _settingsService = settingsService;
        _logger = logger;
    }

    [DisableConcurrentExecution(120)]
    [AutomaticRetry(Attempts = 2, DelaysInSeconds = [300, 900])]
    public async Task RunAsync(CancellationToken ct)
    {
        _logger.LogInformation("Weekly digest generation starting");

        var clinics = await _unitOfWork.Repository<Clinic>().GetAllAsync();
        foreach (var clinic in clinics.Where(c => c.IsActive))
        {
            ct.ThrowIfCancellationRequested();

            var totalDocs = await _unitOfWork.Repository<PolicyDocument>().CountAsync(d => d.ClinicId == clinic.Id);
            var warningDaysStr = await _settingsService.GetValueAsync("PolicyDocument.ExpiryWarningDays");
            var warnDays = int.TryParse(warningDaysStr, out var wd) ? wd : 30;
            var expiringDocs = await _unitOfWork.Repository<PolicyDocument>().CountAsync(
                d => d.ClinicId == clinic.Id && d.ExpiryDate <= DateTime.UtcNow.AddDays(warnDays)
            );

            var notification = new Notification
            {
                ClinicId = clinic.Id,
                Title = "Weekly Compliance Digest",
                Message = $"Clinic '{clinic.Name}': {totalDocs} total documents, {expiringDocs} expiring within {warnDays} days.",
                MessageAr = $"ملخص الامتثال الأسبوعي للعيادة '{clinic.NameAr ?? clinic.Name}': {totalDocs} مستند، {expiringDocs} سينتهي خلال {warnDays} يومًا.",
                NotificationType = NotificationType.ComplianceAlert,
                TargetObjectType = "Clinic",
                TargetObjectId = clinic.Id
            };

            await _unitOfWork.Repository<Notification>().AddAsync(notification);
        }

        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("Weekly digest notifications created for {Count} clinics", clinics.Count(c => c.IsActive));
    }
}
