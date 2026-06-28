using AmbulatoryCarePortal.Application.Interfaces;
using AmbulatoryCarePortal.Domain.Entities;
using AmbulatoryCarePortal.Presentation.Helpers;
using AmbulatoryCarePortal.Presentation.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AmbulatoryCarePortal.Presentation.Areas.ClinicAdmin.Controllers;

[Area("ClinicAdmin")]
[Authorize(Policy = "Permission.dashboard.view")]
public class DashboardController : Controller
{
    private readonly IClinicService _clinicService;
    private readonly IKPIService _kpiService;
    private readonly IHrService _hrService;
    private readonly IComplianceCalendarService _complianceCalendarService;
    private readonly IComplianceScoreService _complianceScoreService;
    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger<DashboardController> _logger;
    private readonly ITranslationService _localizer;

    public DashboardController(
        IClinicService clinicService,
        IKPIService kpiService,
        IHrService hrService,
        IComplianceCalendarService complianceCalendarService,
        IComplianceScoreService complianceScoreService,
        ILogger<DashboardController> logger,
        UserManager<AppUser> userManager,
        ITranslationService localizer)
    {
        _clinicService = clinicService;
        _kpiService = kpiService;
        _hrService = hrService;
        _complianceCalendarService = complianceCalendarService;
        _complianceScoreService = complianceScoreService;
        _logger = logger;
        _userManager = userManager;
        _localizer = localizer;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        var clinicId = user?.ClinicId;

        if (!clinicId.HasValue)
            return Unauthorized();

        var clinic = await _clinicService.GetClinicDetailsAsync(clinicId.Value);
        if (clinic == null)
            return NotFound();

        ViewBag.PageTitle = _localizer.T("Page.ClinicDashboard");

        return View(clinic);
    }

    public async Task<IActionResult> KPIs()
    {
        var user = await _userManager.GetUserAsync(User);
        var clinicId = user?.ClinicId;
        if (!clinicId.HasValue)
            return Unauthorized();

        var kpis = await _kpiService.GetClinicKPIsAsync(clinicId.Value);
        ViewBag.PageTitle = _localizer.T("Page.KPIMonitoring");
        ViewBag.ClinicId = clinicId;

        return View(kpis);
    }

    public async Task<IActionResult> Staff(int page = 1, int pageSize = 10)
    {
        var user = await _userManager.GetUserAsync(User);
        var clinicId = user?.ClinicId;
        if (!clinicId.HasValue)
            return Unauthorized();

        var staff = await _hrService.GetClinicStaffAsync(clinicId.Value, page, pageSize);
        ViewBag.PageTitle = _localizer.T("Page.StaffManagement");
        ViewBag.ClinicId = clinicId;

        return View(staff);
    }

    public async Task<IActionResult> ExpiringDocuments()
    {
        var user = await _userManager.GetUserAsync(User);
        var clinicId = user?.ClinicId;
        if (!clinicId.HasValue)
            return Unauthorized();

        var expiringDocs = await _hrService.GetExpiringDocumentsAsync(clinicId.Value, 30);
        ViewBag.PageTitle = _localizer.T("Page.ExpiringDocuments");
        ViewBag.ClinicId = clinicId;

        return View(expiringDocs);
    }

    public async Task<IActionResult> ComplianceCalendar()
    {
        var user = await _userManager.GetUserAsync(User);
        var clinicId = user?.ClinicId;
        if (!clinicId.HasValue)
            return Unauthorized();

        var model = await _complianceCalendarService.GetCalendarAsync(clinicId.Value);
        ViewBag.PageTitle = _localizer.T("Page.ComplianceCalendar");

        return View(model);
    }

    public async Task<IActionResult> ComplianceScore()
    {
        var user = await _userManager.GetUserAsync(User);
        var clinicId = user?.ClinicId;
        if (!clinicId.HasValue)
            return Unauthorized();

        ViewBag.PageTitle = _localizer.T("Page.ComplianceScore");
        var score = await _complianceScoreService.GetLatestScoreAsync(clinicId.Value);
        var dashboard = await _complianceScoreService.GetDashboardAsync(clinicId.Value);
        return View(new ComplianceScoreWidgetViewModel { Score = score, Dashboard = dashboard });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateComplianceScore()
    {
        var user = await _userManager.GetUserAsync(User);
        var clinicId = user?.ClinicId;
        if (!clinicId.HasValue)
            return Unauthorized();

        try
        {
            var result = await _complianceScoreService.CalculateScoreAsync(clinicId.Value);
            return Json(new { success = true, score = result.OverallScore });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating compliance score");
            return Json(new { success = false, message = "Error calculating compliance score" });
        }
    }

    [HttpGet]
    public async Task<IActionResult> ScoreHistory()
    {
        var user = await _userManager.GetUserAsync(User);
        var clinicId = user?.ClinicId;
        if (!clinicId.HasValue)
            return Unauthorized();

        var history = await _complianceScoreService.GetScoreHistoryAsync(clinicId.Value, 10);
        return Json(history.Select(h => new
        {
            calculatedAt = h.CalculatedAt,
            overallScore = h.OverallScore,
            policyScore = h.PolicyScore,
            kpiScore = h.KpiScore,
            checklistScore = h.ChecklistScore,
            hrScore = h.HrScore,
            documentScore = h.DocumentScore
        }));
    }

    [HttpGet]
    public async Task<IActionResult> ScoreData()
    {
        var user = await _userManager.GetUserAsync(User);
        var clinicId = user?.ClinicId;
        if (!clinicId.HasValue)
            return Unauthorized();

        var score = await _complianceScoreService.GetLatestScoreAsync(clinicId.Value);
        var dashboard = await _complianceScoreService.GetDashboardAsync(clinicId.Value);
        var widget = new ComplianceScoreWidgetViewModel { Score = score, Dashboard = dashboard };
        return Json(new
        {
            overallScore = widget.Score.OverallScore,
            status = widget.Score.OverallScore >= 80 ? "compliant" : widget.Score.OverallScore >= 60 ? "partial" : "noncompliant",
            components = widget.Score.Components.Select(c => new
            {
                name = c.Name,
                score = c.Score,
                weight = c.Weight,
                color = c.Color,
                icon = c.Icon
            }),
            dashboard = new
            {
                missingPolicies = widget.Dashboard.MissingPolicies,
                expiredDocuments = widget.Dashboard.ExpiredDocuments,
                overdueChecklists = widget.Dashboard.OverdueChecklists,
                expiringCredentials = widget.Dashboard.ExpiringCredentials
            },
            lastCalculated = widget.Score.CalculatedAt
        });
    }
}
