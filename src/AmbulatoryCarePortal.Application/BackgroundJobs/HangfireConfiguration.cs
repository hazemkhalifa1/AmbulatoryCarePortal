using Hangfire;

namespace AmbulatoryCarePortal.Application.BackgroundJobs;

public static class HangfireConfiguration
{
    public static void RegisterRecurringJobs(IRecurringJobManager manager)
    {
        manager.AddOrUpdate<DocumentExpiryCheckJob>(
            "document-expiry-check",
            job => job.RunAsync(CancellationToken.None),
            "0 */6 * * *");

        manager.AddOrUpdate<ChecklistReminderJob>(
            "checklist-reminder",
            job => job.RunAsync(CancellationToken.None),
            "0 */12 * * *");

        manager.AddOrUpdate<ComplianceAlertJob>(
            "compliance-alert",
            job => job.RunAsync(CancellationToken.None),
            "0 */8 * * *");

        manager.AddOrUpdate<WeeklyDigestJob>(
            "weekly-digest",
            job => job.RunAsync(CancellationToken.None),
            "0 8 * * 1");

        manager.AddOrUpdate<ComplianceScoreJob>(
            "compliance-score",
            job => job.RunAsync(CancellationToken.None),
            "0 */6 * * *");
    }
}
