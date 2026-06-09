using AmbulatoryCarePortal.Application.Interfaces;
using AmbulatoryCarePortal.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AmbulatoryCarePortal.Presentation.Areas.ClinicAdmin.Controllers;

[Area("ClinicAdmin")]
[Authorize(Roles = "ClinicAdmin,ClinicViewer")]
public class DashboardController : Controller
{
    private readonly IClinicService _clinicService;
    private readonly IPolicyDocumentService _policyDocumentService;
    private readonly IKPIService _kpiService;
    private readonly IHrService _hrService;
    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        IClinicService clinicService,
        IPolicyDocumentService policyDocumentService,
        IKPIService kpiService,
        IHrService hrService,
        ILogger<DashboardController> logger,
        UserManager<AppUser> userManager)
    {
        _clinicService = clinicService;
        _policyDocumentService = policyDocumentService;
        _kpiService = kpiService;
        _hrService = hrService;
        _logger = logger;
        _userManager = userManager;
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

        ViewBag.PageTitle = "Clinic Dashboard";

        return View(clinic);
    }

    public async Task<IActionResult> Policies(int page = 1, int pageSize = 10)
    {
        var user = await _userManager.GetUserAsync(User);
        var clinicId = user?.ClinicId;
        if (!clinicId.HasValue)
            return Unauthorized();

        var policies = await _policyDocumentService.GetClinicPoliciesAsync(clinicId.Value, page, pageSize);
        ViewBag.PageTitle = "Policy Documents";
        ViewBag.ClinicId = clinicId;

        return View(policies);
    }

    public async Task<IActionResult> KPIs()
    {
        var user = await _userManager.GetUserAsync(User);
        var clinicId = user?.ClinicId;
        if (!clinicId.HasValue)
            return Unauthorized();

        var kpis = await _kpiService.GetClinicKPIsAsync(clinicId.Value);
        ViewBag.PageTitle = "KPI Monitoring";
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
        ViewBag.PageTitle = "HR Staff Management";
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
        ViewBag.PageTitle = "Expiring Documents";
        ViewBag.ClinicId = clinicId;

        return View(expiringDocs);
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
            var score = await _clinicService.CalculateComplianceScoreAsync(clinicId.Value);
            return Json(new { success = true, score });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating compliance score");
            return Json(new { success = false, message = "Error calculating compliance score" });
        }
    }
}
