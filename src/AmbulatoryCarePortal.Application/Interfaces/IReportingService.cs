namespace AmbulatoryCarePortal.Application.Interfaces;

public interface IReportingService
{
    Task<byte[]> GenerateComplianceReportAsync(int clinicId, DateTime startDate, DateTime endDate, string format);
    Task<byte[]> GenerateKPIReportAsync(int clinicId, DateTime startDate, DateTime endDate, string format);
    Task<byte[]> GenerateAuditReportAsync(int clinicId, DateTime startDate, DateTime endDate, string format);
    Task<byte[]> GenerateChecklistReportAsync(int clinicId, DateTime startDate, DateTime endDate, string format);
    Task<byte[]> GenerateHRReportAsync(int clinicId, DateTime startDate, DateTime endDate, string format);
    Task<List<string>> GetAvailableReportsAsync(string userRole);
}
