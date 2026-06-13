using AmbulatoryCarePortal.Application.BackgroundJobs;
using AmbulatoryCarePortal.Application.Interfaces;
using Hangfire;

namespace AmbulatoryCarePortal.Application.Services;

public class BackgroundJobService : IBackgroundJobService
{
    public Task ScheduleDocumentExpiryCheckAsync()
    {
        BackgroundJob.Enqueue<DocumentExpiryCheckJob>(job => job.RunAsync(CancellationToken.None));
        return Task.CompletedTask;
    }

    public Task ScheduleChecklistRemindersAsync()
    {
        BackgroundJob.Enqueue<ChecklistReminderJob>(job => job.RunAsync(CancellationToken.None));
        return Task.CompletedTask;
    }

    public Task ScheduleWeeklyDigestAsync()
    {
        BackgroundJob.Enqueue<WeeklyDigestJob>(job => job.RunAsync(CancellationToken.None));
        return Task.CompletedTask;
    }

    public Task ScheduleComplianceAlertAsync()
    {
        BackgroundJob.Enqueue<ComplianceAlertJob>(job => job.RunAsync(CancellationToken.None));
        return Task.CompletedTask;
    }

    public Task ScheduleReportGenerationAsync()
    {
        BackgroundJob.Enqueue<WeeklyDigestJob>(job => job.RunAsync(CancellationToken.None));
        return Task.CompletedTask;
    }
}
