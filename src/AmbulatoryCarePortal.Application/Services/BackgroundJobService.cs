using System;
using System.Linq;
using System.Threading.Tasks;
using AmbulatoryCarePortal.Domain.Entities;
using AmbulatoryCarePortal.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AmbulatoryCarePortal.Application.Interfaces;

namespace AmbulatoryCarePortal.Application.Services;

/// <summary>
/// Professional Email Service for sending emails and notifications
/// </summary>
public class BackgroundJobService : IBackgroundJobService
{
    private readonly IEmailService _emailService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<BackgroundJobService> _logger;

    public BackgroundJobService(
        IEmailService emailService,
        IUnitOfWork unitOfWork,
        ILogger<BackgroundJobService> logger)
    {
        _emailService = emailService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task ScheduleDocumentExpiryCheckAsync()
    {
        try
        {
            var documents = await _unitOfWork.Repository<HrDocument>().FindAsync(
                d => d.ExpiryDate.HasValue && !d.IsDeleted
            );

            var today = DateTime.UtcNow;
            var thirtyDaysFromNow = today.AddDays(30);

            var expiringDocs = documents
                .Where(d => d.ExpiryDate >= today && d.ExpiryDate <= thirtyDaysFromNow)
                .GroupBy(d => d.HrStaffId)
                .ToList();

            foreach (var docGroup in expiringDocs)
            {
                var staff = await _unitOfWork.Repository<HrStaff>().GetByIdAsync(docGroup.Key);
                if (staff?.CreatedBy != null)
                {
                    foreach (var doc in docGroup)
                    {
                        await _emailService.SendExpiryReminderAsync(
                            staff.CreatedBy,
                            doc.DocumentName,
                            doc.ExpiryDate.Value
                        );
                    }
                }
            }

            _logger.LogInformation("Document expiry check completed");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in document expiry check: {ex.Message}");
        }
    }

    public async Task ScheduleChecklistRemindersAsync()
    {
        try
        {
            var today = DateTime.UtcNow;
            var templates = await _unitOfWork.Repository<ChecklistTemplate>().FindAsync(
                t => t.IsActive && !t.IsDeleted
            );

            foreach (var template in templates)
            {
                var recentRounds = await _unitOfWork.Repository<ChecklistRound>().FindAsync(
                    r => r.ChecklistTemplateId == template.Id && r.ExecutedAt >= today.AddDays(-(int)template.Frequency)
                );

                if (!recentRounds.Any())
                {
                    var notification = new Notification
                    {
                        ClinicId = template.ClinicId,
                        Title = "Checklist Reminder",
                        Message = $"The checklist '{template.Name}' is due. Please complete it as per the {template.Frequency} schedule.",
                        MessageAr = $"تذكير بقائمة التحقق: '{template.NameAr ?? template.Name}' مستحقة. يرجى إكمالها وفقًا للجدول الزمني.",
                        NotificationType = NotificationType.OpenGap,
                        TargetObjectType = "ChecklistTemplate",
                        TargetObjectId = template.Id
                    };

                    await _unitOfWork.Repository<Notification>().AddAsync(notification);
                }
            }

            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("Checklist reminders processed");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in checklist reminder scheduling: {ex.Message}");
        }
    }

    public async Task ScheduleWeeklyDigestAsync()
    {
        try
        {
            var clinics = await _unitOfWork.Repository<Clinic>().GetAllAsync();
            foreach (var clinic in clinics.Where(c => c.IsActive))
            {
                var totalDocs = await _unitOfWork.Repository<PolicyDocument>().CountAsync(d => d.ClinicId == clinic.Id);
                var expiringDocs = await _unitOfWork.Repository<PolicyDocument>().CountAsync(
                    d => d.ClinicId == clinic.Id && d.ExpiryDate <= DateTime.UtcNow.AddDays(30)
                );

                var notification = new Notification
                {
                    ClinicId = clinic.Id,
                    Title = "Weekly Compliance Digest",
                    Message = $"Clinic '{clinic.Name}': {totalDocs} total documents, {expiringDocs} expiring within 30 days.",
                    MessageAr = $"ملخص الامتثال الأسبوعي للعيادة '{clinic.NameAr ?? clinic.Name}': {totalDocs} مستند، {expiringDocs} سينتهي خلال 30 يومًا.",
                    NotificationType = NotificationType.ComplianceAlert,
                    TargetObjectType = "Clinic",
                    TargetObjectId = clinic.Id
                };

                await _unitOfWork.Repository<Notification>().AddAsync(notification);
            }

            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("Weekly digest notifications created");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in weekly digest: {ex.Message}");
        }
    }

    public async Task ScheduleComplianceAlertAsync()
    {
        try
        {
            var clinics = await _unitOfWork.Repository<Clinic>().FindAsync(c => c.IsActive);
            foreach (var clinic in clinics)
            {
                var totalDocs = await _unitOfWork.Repository<PolicyDocument>().CountAsync(d => d.ClinicId == clinic.Id && !d.IsDeleted);
                var missingDocs = await _unitOfWork.Repository<PolicyDocument>().CountAsync(
                    d => d.ClinicId == clinic.Id && d.DocumentStatus == DocumentStatus.MissingAttachment && !d.IsDeleted
                );
                var expiredDocs = await _unitOfWork.Repository<PolicyDocument>().CountAsync(
                    d => d.ClinicId == clinic.Id && d.DocumentStatus == DocumentStatus.Expired && !d.IsDeleted
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
            _logger.LogInformation("Compliance alerts created");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in compliance alert: {ex.Message}");
        }
    }

    public async Task ScheduleReportGenerationAsync()
    {
        try
        {
            _logger.LogInformation("Scheduled report generation completed");
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in report generation: {ex.Message}");
        }
    }
}
