using AmbulatoryCarePortal.Application.Interfaces;
using AmbulatoryCarePortal.Domain.Entities;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace AmbulatoryCarePortal.Application.BackgroundJobs;

public class DocumentExpiryCheckJob
{
    private readonly IEmailService _emailService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<DocumentExpiryCheckJob> _logger;

    public DocumentExpiryCheckJob(
        IEmailService emailService,
        IUnitOfWork unitOfWork,
        ISettingsService settingsService,
        ILogger<DocumentExpiryCheckJob> logger)
    {
        _emailService = emailService;
        _unitOfWork = unitOfWork;
        _settingsService = settingsService;
        _logger = logger;
    }

    [DisableConcurrentExecution(60)]
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = [60, 300, 600])]
    public async Task RunAsync(CancellationToken ct)
    {
        _logger.LogInformation("Document expiry check starting");

        var warningDaysStr = await _settingsService.GetValueAsync("HRDocument.ExpiryWarningDays");
        var warningDays = int.TryParse(warningDaysStr, out var wd) ? wd : 30;
        var today = DateTime.UtcNow;
        var expiryThreshold = today.AddDays(warningDays);

        var expiringDocs = await _unitOfWork.Repository<HrDocument>().FindAsync(
            d => d.ExpiryDate.HasValue &&
                 d.ExpiryDate >= today &&
                 d.ExpiryDate <= expiryThreshold &&
                 !d.IsDeleted,
            includeDeleted: false
        );

        var groups = expiringDocs
            .GroupBy(d => d.HrStaffId)
            .ToList();

        var staffIds = groups.Select(g => g.Key).ToList();
        var staffLookup = (await _unitOfWork.Repository<HrStaff>().FindAsync(s => staffIds.Contains(s.Id)))
            .ToDictionary(s => s.Id);

        var allDocIds = expiringDocs.Select(d => d.Id).ToList();
        var existingNotifications = await _unitOfWork.Repository<Notification>().FindAsync(
            n => n.TargetObjectType == "HrDocument" && n.TargetObjectId.HasValue && allDocIds.Contains(n.TargetObjectId.Value)
        );
        var notifiedDocIds = new HashSet<int>(existingNotifications.Select(n => n.TargetObjectId!.Value));

        foreach (var docGroup in groups)
        {
            ct.ThrowIfCancellationRequested();

            if (!staffLookup.TryGetValue(docGroup.Key, out var staff) || staff.CreatedBy == null)
                continue;

            foreach (var doc in docGroup)
            {
                if (notifiedDocIds.Contains(doc.Id))
                    continue;

                await _emailService.SendExpiryReminderAsync(
                    staff.CreatedBy,
                    doc.DocumentName,
                    doc.ExpiryDate.Value
                );
            }
        }

        _logger.LogInformation("Document expiry check completed — {Count} expiring groups", groups.Count);
    }
}
