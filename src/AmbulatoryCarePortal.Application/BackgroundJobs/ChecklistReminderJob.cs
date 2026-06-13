using System.Linq;
using AmbulatoryCarePortal.Application.Interfaces;
using AmbulatoryCarePortal.Domain.Entities;
using AmbulatoryCarePortal.Domain.Enums;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace AmbulatoryCarePortal.Application.BackgroundJobs;

public class ChecklistReminderJob
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ChecklistReminderJob> _logger;

    public ChecklistReminderJob(
        IUnitOfWork unitOfWork,
        ILogger<ChecklistReminderJob> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    [DisableConcurrentExecution(60)]
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = [60, 300, 600])]
    public async Task RunAsync(CancellationToken ct)
    {
        _logger.LogInformation("Checklist reminder check starting");

        var today = DateTime.UtcNow;
        var templates = await _unitOfWork.Repository<ChecklistTemplate>().FindAsync(
            t => t.IsActive && !t.IsDeleted
        );

        foreach (var template in templates)
        {
            ct.ThrowIfCancellationRequested();

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
        _logger.LogInformation("Checklist reminders processed — {Count} templates checked", templates.Count());
    }
}
