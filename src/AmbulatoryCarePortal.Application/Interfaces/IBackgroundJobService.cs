namespace AmbulatoryCarePortal.Application.Interfaces;

public interface IBackgroundJobService
{
    Task ScheduleDocumentExpiryCheckAsync();
    Task ScheduleChecklistRemindersAsync();
    Task ScheduleWeeklyDigestAsync();
    Task ScheduleComplianceAlertAsync();
    Task ScheduleReportGenerationAsync();
}
