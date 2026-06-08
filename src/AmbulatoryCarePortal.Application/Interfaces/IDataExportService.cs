namespace AmbulatoryCarePortal.Application.Interfaces;

public interface IDataExportService
{
    Task<byte[]> ExportToExcelAsync<T>(List<T> data, string sheetName) where T : class;
    Task<byte[]> ExportToPdfAsync<T>(List<T> data, string reportTitle) where T : class;
    Task<byte[]> ExportToCsvAsync<T>(List<T> data) where T : class;
    Task<string> ExportToJsonAsync<T>(List<T> data) where T : class;
    Task<byte[]> ExportPoliciesAsync(int clinicId, string format);
    Task<byte[]> ExportKPIsAsync(int clinicId, string format);
    Task<byte[]> ExportStaffAsync(int clinicId, string format);
    Task<byte[]> ExportAuditLogsAsync(int clinicId, DateTime startDate, DateTime endDate, string format);
}
