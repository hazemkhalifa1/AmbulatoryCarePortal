using AmbulatoryCarePortal.Application.Interfaces;
using AmbulatoryCarePortal.Domain.Entities;
using AmbulatoryCarePortal.Presentation.Helpers;
using AmbulatoryCarePortal.Presentation.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AmbulatoryCarePortal.Presentation.Areas.SuperAdmin.Controllers;

[Area("SuperAdmin")]
[Authorize(Policy = "Permission.system.configure")]
public class DashboardController : Controller
{
    private readonly IClinicService _clinicService;
    private readonly IAuditService _auditService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITranslationService _localizer;

    public DashboardController(
        IClinicService clinicService,
        IAuditService auditService,
        IUnitOfWork unitOfWork,
        ITranslationService localizer)
    {
        _clinicService = clinicService;
        _auditService = auditService;
        _unitOfWork = unitOfWork;
        _localizer = localizer;
    }

    public async Task<IActionResult> Index()
    {
        var clinics = await _clinicService.GetAllClinicsAsync(1, 10);

        var allClinics = await _unitOfWork.Repository<Clinic>().GetAllAsync();

        var avgCompliance = allClinics.Any()
            ? Math.Round(allClinics.Average(c => c.ComplianceScore), 1)
            : 0m;

        var viewModel = new SuperAdminDashboardViewModel
        {
            Metrics = new DashboardMetricsViewModel
            {
                TotalClinics = allClinics.Count(),
                AverageCompliance = avgCompliance,
                PendingApprovals = 0,
                OverdueItems = 0
            },
            RecentClinics = clinics.Data
        };

        ViewBag.PageTitle = _localizer.T("Page.Dashboard");

        return View(viewModel);
    }

    public async Task<IActionResult> Clinics(int page = 1, int pageSize = 10)
    {
        var clinics = await _clinicService.GetAllClinicsAsync(page, pageSize);
        ViewBag.PageTitle = _localizer.T("Page.ManageClinics");

        return View(clinics);
    }

    public async Task<IActionResult> AuditLog(int clinicId, int page = 1, int pageSize = 20, string? searchTerm = null, string? actionTypeFilter = null, DateTime? dateFrom = null, DateTime? dateTo = null)
    {
        var auditTrail = await _auditService.GetAuditTrailAsync(clinicId, page, pageSize, searchTerm, actionTypeFilter, dateFrom, dateTo);
        ViewBag.PageTitle = _localizer.T("Page.AuditLog");
        ViewBag.ClinicId = clinicId;

        return View(auditTrail);
    }
}
