using System.Linq;
using AmbulatoryCarePortal.Application.Interfaces;
using AmbulatoryCarePortal.Domain.Entities;
using AmbulatoryCarePortal.Domain.Enums;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace AmbulatoryCarePortal.Application.BackgroundJobs;

public class ComplianceAlertJob
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ComplianceAlertJob> _logger;

    public ComplianceAlertJob(
        IUnitOfWork unitOfWork,
        ILogger<ComplianceAlertJob> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    [DisableConcurrentExecution(60)]
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = [60, 300, 600])]
    public async Task RunAsync(CancellationToken ct)
    {
        _logger.LogInformation("Compliance alert check starting");

        var clinics = await _unitOfWork.Repository<Clinic>().FindAsync(c => c.IsActive);
        var clinicIds = clinics.Select(c => c.Id).ToList();

        var existingAlerts = await _unitOfWork.Repository<Notification>().FindAsync(
            n => n.NotificationType == NotificationType.ComplianceAlert &&
                 n.Title == "Compliance Alert" &&
                 clinicIds.Contains(n.ClinicId) &&
                 n.CreatedAt >= DateTime.UtcNow.AddDays(-1)
        );
        var alertedClinicIds = new HashSet<int>(existingAlerts.Select(n => n.ClinicId));

        foreach (var clinic in clinics)
        {
            ct.ThrowIfCancellationRequested();

            if (alertedClinicIds.Contains(clinic.Id))
                continue;

            var missingDocs = await _unitOfWork.Repository<ClinicTemplateAssignment>().CountAsync(
                d => d.ClinicId == clinic.Id && d.AssignmentStatus == ClinicDocumentStatus.MissingAttachment && !d.IsDeleted
            );
            var expiredDocs = await _unitOfWork.Repository<ClinicTemplateAssignment>().CountAsync(
                d => d.ClinicId == clinic.Id && d.AssignmentStatus == ClinicDocumentStatus.Expired && !d.IsDeleted
            );

            if (missingDocs > 5 || expiredDocs > 3)
            {
                var notification = new Notification
                {
                    ClinicId = clinic.Id,
                    Title = "Compliance Alert",
                    Message = $"Clinic '{clinic.Name}' has {missingDocs} missing attachments and {expiredDocs} expired documents.",
                    MessageAr = $"تنبيه امتثال: العيادة '{clinic.NameAr ?? clinic.Name}' لديها {missingDocs} مرفق ناقص و{expiredDocs} مستند منتهي.",
                    NotificationType = NotificationType.ComplianceAlert,
                    TargetObjectType = "Clinic",
                    TargetObjectId = clinic.Id
                };

                await _unitOfWork.Repository<Notification>().AddAsync(notification);
            }
        }

        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("Compliance alerts created for {Count} clinics", clinics.Count());
    }
}
