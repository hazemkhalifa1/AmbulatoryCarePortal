using AmbulatoryCarePortal.Application.Interfaces;
using AmbulatoryCarePortal.Domain.Entities;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AmbulatoryCarePortal.Application.Services;

public class DataExportService : IDataExportService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DataExportService> _logger;

    public DataExportService(IUnitOfWork unitOfWork, ILogger<DataExportService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<byte[]> ExportToExcelAsync<T>(List<T> data, string sheetName) where T : class
    {
        try
        {
            var content = GenerateExcelContent(data, sheetName);
            _logger.LogInformation($"Excel export generated for {typeof(T).Name}");
            return System.Text.Encoding.UTF8.GetBytes(content);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error exporting to Excel: {ex.Message}");
            throw;
        }
    }

    public async Task<byte[]> ExportToPdfAsync<T>(List<T> data, string reportTitle) where T : class
    {
        try
        {
            var content = GeneratePdfContent(data, reportTitle);
            _logger.LogInformation($"PDF export generated for {typeof(T).Name}");
            return System.Text.Encoding.UTF8.GetBytes(content);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error exporting to PDF: {ex.Message}");
            throw;
        }
    }

    public async Task<byte[]> ExportToCsvAsync<T>(List<T> data) where T : class
    {
        try
        {
            var content = GenerateCsvContent(data);
            _logger.LogInformation($"CSV export generated for {typeof(T).Name}");
            return System.Text.Encoding.UTF8.GetBytes(content);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error exporting to CSV: {ex.Message}");
            throw;
        }
    }

    public async Task<string> ExportToJsonAsync<T>(List<T> data) where T : class
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            var json = JsonSerializer.Serialize(data, options);
            _logger.LogInformation($"JSON export generated for {typeof(T).Name}");
            return json;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error exporting to JSON: {ex.Message}");
            throw;
        }
    }

    public async Task<byte[]> ExportPoliciesAsync(int clinicId, string format)
    {
        try
        {
            var policies = await _unitOfWork.Repository<PolicyDocument>().FindAsync(
                p => p.ClinicId == clinicId
            );

            var data = policies.Select(p => new
            {
                p.Id,
                p.Title,
                p.StandardCode,
                Status = p.DocumentStatus.ToString(),
                p.ExpiryDate,
                p.CreatedAt,
                Version = p.VersionNumber
            }).ToList();

            return format.ToLower() == "pdf"
                ? await ExportToPdfAsync(data, "Policy Export")
                : await ExportToExcelAsync(data, "Policies");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error exporting policies: {ex.Message}");
            throw;
        }
    }

    public async Task<byte[]> ExportKPIsAsync(int clinicId, string format)
    {
        try
        {
            var kpis = await _unitOfWork.Repository<KPI>().FindAsync(k => k.ClinicId == clinicId);

            var data = kpis.Select(k => new
            {
                k.Id,
                k.Name,
                k.TargetValue,
                Frequency = k.Frequency.ToString(),
                k.CreatedAt
            }).ToList();

            return format.ToLower() == "pdf"
                ? await ExportToPdfAsync(data, "KPI Export")
                : await ExportToExcelAsync(data, "KPIs");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error exporting KPIs: {ex.Message}");
            throw;
        }
    }

    public async Task<byte[]> ExportStaffAsync(int clinicId, string format)
    {
        try
        {
            var staff = await _unitOfWork.Repository<HrStaff>().FindAsync(s => s.ClinicId == clinicId);

            var data = staff.Select(s => new
            {
                s.Id,
                s.FullNameEn,
                s.FullNameAr,
                s.Email,
                s.Phone,
                StaffType = s.StaffType.ToString(),
                Status = s.IsActive ? "Active" : "Inactive"
            }).ToList();

            return format.ToLower() == "pdf"
                ? await ExportToPdfAsync(data, "Staff Export")
                : await ExportToExcelAsync(data, "Staff");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error exporting staff: {ex.Message}");
            throw;
        }
    }

    public async Task<byte[]> ExportAuditLogsAsync(int clinicId, DateTime startDate, DateTime endDate, string format)
    {
        try
        {
            var auditLogs = await _unitOfWork.Repository<AuditTrail>().FindAsync(
                a => a.ClinicId == clinicId && a.ActionDate >= startDate && a.ActionDate <= endDate
            );

            var data = auditLogs.Select(a => new
            {
                a.Id,
                Action = a.ActionType.ToString(),
                a.Description,
                a.CreatedBy,
                a.ActionDate,
                a.IpAddress
            }).ToList();

            return format.ToLower() == "pdf"
                ? await ExportToPdfAsync(data, "Audit Log Export")
                : await ExportToExcelAsync(data, "Audit Logs");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error exporting audit logs: {ex.Message}");
            throw;
        }
    }

    private string GenerateExcelContent<T>(List<T> data, string sheetName) where T : class
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Sheet Name: {sheetName}");
        sb.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();

        if (data.Any())
        {
            var properties = typeof(T).GetProperties();
            sb.AppendLine(string.Join(",", properties.Select(p => p.Name)));

            foreach (var item in data)
                sb.AppendLine(string.Join(",", properties.Select(p => p.GetValue(item)?.ToString() ?? "")));
        }

        return sb.ToString();
    }

    private string GeneratePdfContent<T>(List<T> data, string reportTitle) where T : class
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"=== {reportTitle} ===");
        sb.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"Total Records: {data.Count()}");
        sb.AppendLine();

        foreach (var item in data)
        {
            var properties = typeof(T).GetProperties();
            foreach (var prop in properties)
                sb.AppendLine($"{prop.Name}: {prop.GetValue(item)}");
            sb.AppendLine("---");
        }

        return sb.ToString();
    }

    private string GenerateCsvContent<T>(List<T> data) where T : class
    {
        var sb = new System.Text.StringBuilder();

        if (data.Any())
        {
            var properties = typeof(T).GetProperties();
            sb.AppendLine(string.Join(",", properties.Select(p => $"\"{p.Name}\"")));

            foreach (var item in data)
                sb.AppendLine(string.Join(",", properties.Select(p => $"\"{p.GetValue(item)?.ToString() ?? ""}\"")));
        }

        return sb.ToString();
    }
}
