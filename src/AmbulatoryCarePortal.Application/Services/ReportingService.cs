using AmbulatoryCarePortal.Application.DTOs.Analytics;
using AmbulatoryCarePortal.Application.Interfaces;
using AmbulatoryCarePortal.Application.DTOs;
using AmbulatoryCarePortal.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace AmbulatoryCarePortal.Application.Services;

public class ReportingService : IReportingService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ReportingService> _logger;

    public ReportingService(IUnitOfWork unitOfWork, ILogger<ReportingService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<byte[]> GenerateComplianceReportAsync(int clinicId, DateTime startDate, DateTime endDate, string format)
    {
        try
        {
            var clinic = await _unitOfWork.Repository<Clinic>().GetByIdAsync(clinicId);
            if (clinic == null)
                throw new Exception("Clinic not found");

            var policies = await _unitOfWork.Repository<PolicyDocument>().FindAsync(
                p => p.ClinicId == clinicId && p.CreatedAt >= startDate && p.CreatedAt <= endDate
            );

            var reportData = new
            {
                ClinicName = clinic.Name,
                ReportDate = DateTime.Now,
                ComplianceScore = clinic.ComplianceScore,
                TotalPolicies = policies.Count(),
                ApprovedPolicies = policies.Count(p => p.DocumentStatus == Domain.Enums.DocumentStatus.Approved),
                PendingPolicies = policies.Count(p => p.DocumentStatus == Domain.Enums.DocumentStatus.Pending)
            };

            _logger.LogInformation($"Compliance report generated for clinic {clinicId}");

            return format.ToLower() == "pdf"
                ? GeneratePDFReport(reportData)
                : GenerateExcelReport(reportData);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error generating compliance report: {ex.Message}");
            throw;
        }
    }

    public async Task<byte[]> GenerateKPIReportAsync(int clinicId, DateTime startDate, DateTime endDate, string format)
    {
        try
        {
            var kpis = await _unitOfWork.Repository<KPI>().FindAsync(
                k => k.ClinicId == clinicId && k.CreatedAt >= startDate && k.CreatedAt <= endDate
            );

            var reportData = new
            {
                ClinicId = clinicId,
                ReportDate = DateTime.Now,
                TotalKPIs = kpis.Count(),
                AverageAchievement = kpis.Any() ? kpis.Average(k => 0) : 0
            };

            _logger.LogInformation($"KPI report generated for clinic {clinicId}");

            return format.ToLower() == "pdf"
                ? GeneratePDFReport(reportData)
                : GenerateExcelReport(reportData);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error generating KPI report: {ex.Message}");
            throw;
        }
    }

    public async Task<byte[]> GenerateAuditReportAsync(int clinicId, DateTime startDate, DateTime endDate, string format)
    {
        try
        {
            var auditLogs = await _unitOfWork.Repository<AuditTrail>().FindAsync(
                a => a.ClinicId == clinicId && a.ActionDate >= startDate && a.ActionDate <= endDate
            );

            var reportData = new
            {
                ClinicId = clinicId,
                ReportDate = DateTime.Now,
                TotalActions = auditLogs.Count(),
                Users = auditLogs.Select(a => a.CreatedBy).Distinct().Count()
            };

            _logger.LogInformation($"Audit report generated for clinic {clinicId}");

            return format.ToLower() == "pdf"
                ? GeneratePDFReport(reportData)
                : GenerateExcelReport(reportData);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error generating audit report: {ex.Message}");
            throw;
        }
    }

    public async Task<byte[]> GenerateChecklistReportAsync(int clinicId, DateTime startDate, DateTime endDate, string format)
    {
        try
        {
            var checklists = await _unitOfWork.Repository<ChecklistRound>().FindAsync(
                c => c.ClinicId == clinicId && c.ExecutedAt >= startDate && c.ExecutedAt <= endDate
            );

            var reportData = new
            {
                ClinicId = clinicId,
                ReportDate = DateTime.Now,
                TotalChecklistRounds = checklists.Count()
            };

            _logger.LogInformation($"Checklist report generated for clinic {clinicId}");

            return format.ToLower() == "pdf"
                ? GeneratePDFReport(reportData)
                : GenerateExcelReport(reportData);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error generating checklist report: {ex.Message}");
            throw;
        }
    }

    public async Task<byte[]> GenerateHRReportAsync(int clinicId, DateTime startDate, DateTime endDate, string format)
    {
        try
        {
            var staff = await _unitOfWork.Repository<HrStaff>().FindAsync(
                s => s.ClinicId == clinicId && s.CreatedAt >= startDate && s.CreatedAt <= endDate
            );

            var reportData = new
            {
                ClinicId = clinicId,
                ReportDate = DateTime.Now,
                TotalStaff = staff.Count(),
                ActiveStaff = staff.Count(s => s.IsActive)
            };

            _logger.LogInformation($"HR report generated for clinic {clinicId}");

            return format.ToLower() == "pdf"
                ? GeneratePDFReport(reportData)
                : GenerateExcelReport(reportData);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error generating HR report: {ex.Message}");
            throw;
        }
    }

    public async Task<List<string>> GetAvailableReportsAsync(string userRole)
    {
        var availableReports = userRole switch
        {
            "SuperAdmin" => new List<string>
            {
                "Compliance Report", "KPI Report", "Audit Report",
                "Checklist Report", "HR Report", "System Report"
            },
            "ClinicAdmin" => new List<string>
            {
                "Compliance Report", "KPI Report", "Checklist Report", "HR Report"
            },
            "ComplianceOfficer" => new List<string>
            {
                "Compliance Report", "Audit Report", "Checklist Report"
            },
            "HRManager" => new List<string>
            {
                "HR Report"
            },
            "Auditor" => new List<string>
            {
                "Audit Report", "Compliance Report"
            },
            _ => new List<string>()
        };

        return await Task.FromResult(availableReports);
    }

    private byte[] GeneratePDFReport(object data)
    {
        return System.Text.Encoding.UTF8.GetBytes("PDF Report: " + System.Text.Json.JsonSerializer.Serialize(data));
    }

    private byte[] GenerateExcelReport(object data)
    {
        return System.Text.Encoding.UTF8.GetBytes("Excel Report: " + System.Text.Json.JsonSerializer.Serialize(data));
    }
}
