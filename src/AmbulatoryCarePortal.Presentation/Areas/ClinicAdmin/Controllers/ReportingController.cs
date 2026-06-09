using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AutoMapper;
using AmbulatoryCarePortal.Application.Interfaces;
using AmbulatoryCarePortal.Presentation.ViewModels;

namespace AmbulatoryCarePortal.Presentation.Areas.ClinicAdmin.Controllers;

[Area("ClinicAdmin")]
[Authorize(Roles = "ClinicAdmin")]
public class ReportingController : Controller
{
    private readonly IReportingService _reportingService;
    private readonly IAnalyticsService _analyticsService;
    private readonly IEmailService _emailService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<ReportingController> _logger;

    public ReportingController(
        IReportingService reportingService,
        IAnalyticsService analyticsService,
        IEmailService emailService,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<ReportingController> logger)
    {
        _reportingService = reportingService;
        _analyticsService = analyticsService;
        _emailService = emailService;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Reporting dashboard showing available reports
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userRole = User.FindFirst("http://schemas.microsoft.com/ws/2008/06/identity/claims/role")?.Value;
        
        var availableReports = await _reportingService.GetAvailableReportsAsync(userRole);

        var reportTypes = new List<ReportTypeOptionViewModel>
        {
            new ReportTypeOptionViewModel
            {
                Type = "Compliance",
                DisplayName = "Compliance Report",
                Description = "Complete compliance status, policies, checklists, and recommendations",
                SupportedFormats = new[] { "PDF", "Excel", "CSV" },
                RequiredRoles = new[] { "ClinicAdmin" }
            },
            new ReportTypeOptionViewModel
            {
                Type = "KPI",
                DisplayName = "KPI Performance Report",
                Description = "KPI achievement rates, trends, and performance analysis",
                SupportedFormats = new[] { "PDF", "Excel", "CSV" },
                RequiredRoles = new[] { "ClinicAdmin" }
            },
            new ReportTypeOptionViewModel
            {
                Type = "Audit",
                DisplayName = "Audit Trail Report",
                Description = "Complete audit trail, user actions, and system events",
                SupportedFormats = new[] { "PDF", "Excel", "CSV" },
                RequiredRoles = new[] { "ClinicAdmin" }
            },
            new ReportTypeOptionViewModel
            {
                Type = "Checklist",
                DisplayName = "Checklist Execution Report",
                Description = "Checklist completion rates, trends, and analysis",
                SupportedFormats = new[] { "PDF", "Excel", "CSV" },
                RequiredRoles = new[] { "ClinicAdmin" }
            },
            new ReportTypeOptionViewModel
            {
                Type = "HR",
                DisplayName = "HR & Staff Report",
                Description = "Staff directory, document status, and compliance",
                SupportedFormats = new[] { "PDF", "Excel", "CSV" },
                RequiredRoles = new[] { "ClinicAdmin" }
            }
        };

        return View(reportTypes);
    }

    /// <summary>
    /// Generate compliance report
    /// </summary>
    [HttpGet]
    public IActionResult ComplianceReportBuilder()
    {
        var clinicId = int.Parse(User.FindFirst("ClinicId")?.Value ?? "0");

        var model = new ReportGeneratorViewModel
        {
            ReportType = "Compliance",
            StartDate = DateTime.UtcNow.AddMonths(-6),
            EndDate = DateTime.UtcNow,
            Format = "PDF",
            IncludeCharts = true,
            IncludeAuditTrail = true,
            SelectedClinicIds = new[] { clinicId }
        };

        return View("ReportBuilder", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateComplianceReport(ReportGeneratorViewModel model)
    {
        if (!ModelState.IsValid)
            return View("ReportBuilder", model);

        var clinicId = int.Parse(User.FindFirst("ClinicId")?.Value ?? "0");

        try
        {
            var reportContent = await _reportingService.GenerateComplianceReportAsync(
                clinicId,
                model.StartDate,
                model.EndDate,
                model.Format
            );

            var fileName = $"compliance-report-{DateTime.UtcNow:yyyyMMdd-HHmmss}.{(model.Format.ToLower() == "pdf" ? "pdf" : "xlsx")}";

            _logger.LogInformation($"Compliance report generated for clinic {clinicId}");

            return File(reportContent, 
                model.Format.ToLower() == "pdf" ? "application/pdf" : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error generating compliance report: {ex.Message}");
            TempData["ErrorMessage"] = "Failed to generate report. Please try again.";
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Generate KPI report
    /// </summary>
    [HttpGet]
    public IActionResult KPIReportBuilder()
    {
        var clinicId = int.Parse(User.FindFirst("ClinicId")?.Value ?? "0");

        var model = new ReportGeneratorViewModel
        {
            ReportType = "KPI",
            StartDate = DateTime.UtcNow.AddMonths(-6),
            EndDate = DateTime.UtcNow,
            Format = "PDF",
            IncludeCharts = true,
            SelectedClinicIds = new[] { clinicId }
        };

        return View("ReportBuilder", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateKPIReport(ReportGeneratorViewModel model)
    {
        var clinicId = int.Parse(User.FindFirst("ClinicId")?.Value ?? "0");

        try
        {
            var reportContent = await _reportingService.GenerateKPIReportAsync(
                clinicId,
                model.StartDate,
                model.EndDate,
                model.Format
            );

            var fileName = $"kpi-report-{DateTime.UtcNow:yyyyMMdd-HHmmss}.{(model.Format.ToLower() == "pdf" ? "pdf" : "xlsx")}";

            _logger.LogInformation($"KPI report generated for clinic {clinicId}");

            return File(reportContent,
                model.Format.ToLower() == "pdf" ? "application/pdf" : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error generating KPI report: {ex.Message}");
            TempData["ErrorMessage"] = "Failed to generate report. Please try again.";
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Generate audit report
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "ClinicAdmin,ClinicViewer")]
    public IActionResult AuditReportBuilder()
    {
        var clinicId = int.Parse(User.FindFirst("ClinicId")?.Value ?? "0");

        var model = new ReportGeneratorViewModel
        {
            ReportType = "Audit",
            StartDate = DateTime.UtcNow.AddMonths(-12),
            EndDate = DateTime.UtcNow,
            Format = "PDF",
            SelectedClinicIds = new[] { clinicId }
        };

        return View("ReportBuilder", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "ClinicAdmin,ClinicViewer")]
    public async Task<IActionResult> GenerateAuditReport(ReportGeneratorViewModel model)
    {
        var clinicId = int.Parse(User.FindFirst("ClinicId")?.Value ?? "0");

        try
        {
            var reportContent = await _reportingService.GenerateAuditReportAsync(
                clinicId,
                model.StartDate,
                model.EndDate,
                model.Format
            );

            var fileName = $"audit-report-{DateTime.UtcNow:yyyyMMdd-HHmmss}.{(model.Format.ToLower() == "pdf" ? "pdf" : "xlsx")}";

            _logger.LogInformation($"Audit report generated for clinic {clinicId}");

            return File(reportContent,
                model.Format.ToLower() == "pdf" ? "application/pdf" : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error generating audit report: {ex.Message}");
            TempData["ErrorMessage"] = "Failed to generate report. Please try again.";
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Generate checklist report
    /// </summary>
    [HttpGet]
    public IActionResult ChecklistReportBuilder()
    {
        var clinicId = int.Parse(User.FindFirst("ClinicId")?.Value ?? "0");

        var model = new ReportGeneratorViewModel
        {
            ReportType = "Checklist",
            StartDate = DateTime.UtcNow.AddMonths(-3),
            EndDate = DateTime.UtcNow,
            Format = "PDF",
            IncludeCharts = true,
            SelectedClinicIds = new[] { clinicId }
        };

        return View("ReportBuilder", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateChecklistReport(ReportGeneratorViewModel model)
    {
        var clinicId = int.Parse(User.FindFirst("ClinicId")?.Value ?? "0");

        try
        {
            var reportContent = await _reportingService.GenerateChecklistReportAsync(
                clinicId,
                model.StartDate,
                model.EndDate,
                model.Format
            );

            var fileName = $"checklist-report-{DateTime.UtcNow:yyyyMMdd-HHmmss}.{(model.Format.ToLower() == "pdf" ? "pdf" : "xlsx")}";

            _logger.LogInformation($"Checklist report generated for clinic {clinicId}");

            return File(reportContent,
                model.Format.ToLower() == "pdf" ? "application/pdf" : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error generating checklist report: {ex.Message}");
            TempData["ErrorMessage"] = "Failed to generate report. Please try again.";
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Generate HR report
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "ClinicAdmin")]
    public IActionResult HRReportBuilder()
    {
        var clinicId = int.Parse(User.FindFirst("ClinicId")?.Value ?? "0");

        var model = new ReportGeneratorViewModel
        {
            ReportType = "HR",
            StartDate = DateTime.UtcNow.AddMonths(-12),
            EndDate = DateTime.UtcNow,
            Format = "PDF",
            SelectedClinicIds = new[] { clinicId }
        };

        return View("ReportBuilder", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "ClinicAdmin")]
    public async Task<IActionResult> GenerateHRReport(ReportGeneratorViewModel model)
    {
        var clinicId = int.Parse(User.FindFirst("ClinicId")?.Value ?? "0");

        try
        {
            var reportContent = await _reportingService.GenerateHRReportAsync(
                clinicId,
                model.StartDate,
                model.EndDate,
                model.Format
            );

            var fileName = $"hr-report-{DateTime.UtcNow:yyyyMMdd-HHmmss}.{(model.Format.ToLower() == "pdf" ? "pdf" : "xlsx")}";

            _logger.LogInformation($"HR report generated for clinic {clinicId}");

            return File(reportContent,
                model.Format.ToLower() == "pdf" ? "application/pdf" : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error generating HR report: {ex.Message}");
            TempData["ErrorMessage"] = "Failed to generate report. Please try again.";
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Email report to recipients
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EmailReport(string reportType, string recipients)
    {
        var clinicId = int.Parse(User.FindFirst("ClinicId")?.Value ?? "0");

        try
        {
            var recipientList = recipients.Split(',').ToList();

            // Generate report
            var reportContent = reportType switch
            {
                "Compliance" => await _reportingService.GenerateComplianceReportAsync(clinicId, DateTime.UtcNow.AddMonths(-6), DateTime.UtcNow, "PDF"),
                "KPI" => await _reportingService.GenerateKPIReportAsync(clinicId, DateTime.UtcNow.AddMonths(-6), DateTime.UtcNow, "PDF"),
                "Audit" => await _reportingService.GenerateAuditReportAsync(clinicId, DateTime.UtcNow.AddMonths(-12), DateTime.UtcNow, "PDF"),
                "Checklist" => await _reportingService.GenerateChecklistReportAsync(clinicId, DateTime.UtcNow.AddMonths(-3), DateTime.UtcNow, "PDF"),
                "HR" => await _reportingService.GenerateHRReportAsync(clinicId, DateTime.UtcNow.AddMonths(-12), DateTime.UtcNow, "PDF"),
                _ => null
            };

            if (reportContent != null)
            {
                foreach (var recipient in recipientList)
                {
                    await _emailService.SendScheduledReportAsync(recipient, $"{reportType} Report", reportContent);
                }

                _logger.LogInformation($"{reportType} report sent to {recipientList.Count} recipients");
                TempData["SuccessMessage"] = "Report sent successfully!";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error sending report: {ex.Message}");
            TempData["ErrorMessage"] = "Failed to send report. Please try again.";
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Compliance analytics dashboard
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Analytics()
    {
        var clinicId = int.Parse(User.FindFirst("ClinicId")?.Value ?? "0");

        var overview = await _analyticsService.GetComplianceAnalyticsAsync(clinicId);
        var insights = await _analyticsService.GetComplianceInsightsAsync(clinicId);
        var trends = await _analyticsService.GetComplianceTrendsAsync(clinicId);

        var model = new AnalyticsViewModel
        {
            AnalyticsType = "Compliance",
            ChartData = new List<ChartDataViewModel>
            {
                new ChartDataViewModel
                {
                    ChartType = "Doughnut",
                    Title = "Compliance Status",
                    Labels = new object[] { "Compliant", "Non-Compliant" },
                    Values = new object[] { overview.PolicyCompletionRate, 100 - overview.PolicyCompletionRate }
                }
            },
            Insights = insights.Select(i => new InsightViewModel
            {
                Title = i.Title,
                Description = i.Description,
                Type = i.Type,
                Recommendation = i.Recommendation
            }).ToList(),
            Trends = trends.Select(t => new TrendViewModel
            {
                Metric = t.Metric,
                ChangePercentage = t.ChangePercentage,
                Direction = t.Direction,
                Period = t.Period
            }).ToList()
        };

        return View(model);
    }

    /// <summary>
    /// Get report summary API endpoint
    /// </summary>
    [HttpGet]
    [Route("api/reporting/summary")]
    public async Task<IActionResult> GetReportSummary()
    {
        var clinicId = int.Parse(User.FindFirst("ClinicId")?.Value ?? "0");

        var analytics = await _analyticsService.GetDashboardMetricsAsync(clinicId, User.FindFirst("http://schemas.microsoft.com/ws/2008/06/identity/claims/role")?.Value);

        return Json(analytics);
    }
}
