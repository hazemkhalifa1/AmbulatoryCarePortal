using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AmbulatoryCarePortal.Application.Interfaces;
using AmbulatoryCarePortal.Domain.Enums;
using AmbulatoryCarePortal.Domain.Entities;
using AmbulatoryCarePortal.Presentation.ViewModels;
using AmbulatoryCarePortal.Presentation.Helpers;

namespace AmbulatoryCarePortal.Presentation.Areas.SuperAdmin.Controllers;

[Area("SuperAdmin")]
[Authorize(Roles = "SuperAdmin")]
public class DashboardController : Controller
{
    private readonly IClinicService _clinicService;
    private readonly IAuditService _auditService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DashboardController> _logger;
    private readonly ITranslationService _localizer;

    public DashboardController(
        IClinicService clinicService,
        IAuditService auditService,
        IUnitOfWork unitOfWork,
        ILogger<DashboardController> logger,
        ITranslationService localizer)
    {
        _clinicService = clinicService;
        _auditService = auditService;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _localizer = localizer;
    }

    public async Task<IActionResult> Index()
    {
        var clinics = await _clinicService.GetAllClinicsAsync(1, 10);
        ViewBag.TotalClinics = clinics.TotalCount;
        ViewBag.PageTitle = _localizer.T("Page.Dashboard");

        return View(clinics.Data);
    }

    public async Task<IActionResult> Clinics(int page = 1, int pageSize = 10)
    {
        var clinics = await _clinicService.GetAllClinicsAsync(page, pageSize);
        ViewBag.PageTitle = _localizer.T("Page.ManageClinics");

        return View(clinics);
    }

    public async Task<IActionResult> ClinicDetail(int id)
    {
        var clinic = await _clinicService.GetClinicDetailsAsync(id);
        if (clinic == null)
            return NotFound();

        ViewBag.PageTitle = clinic.Name;

        return View(clinic);
    }

    [HttpGet]
    public IActionResult CreateClinic()
    {
        ViewBag.PageTitle = _localizer.T("Page.CreateClinic");
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateClinic(CreateClinicViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.PageTitle = _localizer.T("Page.CreateClinic");
            return View(model);
        }

        try
        {
            var dto = new AmbulatoryCarePortal.Application.DTOs.Clinic.CreateClinicDto
            {
                Name = model.Name,
                NameAr = model.NameAr,
                CityEn = model.CityEn,
                CityAr = model.CityAr,
                ClinicType = model.ClinicType,
                LicenseNumber = model.LicenseNumber,
                LicenseExpiry = model.LicenseExpiry
            };

            var clinicId = await _clinicService.CreateClinicAsync(dto);

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var auditLog = new AuditTrail
            {
                ActionType = AuditActionType.Create,
                TargetObjectId = clinicId,
                TargetObjectType = nameof(Clinic),
                Description = $"Created clinic: {model.Name}",
                CreatedBy = userId,
                ActionDate = DateTime.UtcNow,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            };

            await _unitOfWork.Repository<AuditTrail>().AddAsync(auditLog);
            await _unitOfWork.SaveChangesAsync();

            TempData["SuccessMessage"] = _localizer.T("Alert.Success.ClinicCreated");

            return RedirectToAction("ClinicDetail", new { id = clinicId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating clinic");
            ModelState.AddModelError(string.Empty, _localizer.T("Alert.Error.ClinicCreateFailed"));
            ViewBag.PageTitle = _localizer.T("Page.CreateClinic");
            return View(model);
        }
    }

    public async Task<IActionResult> AuditLog(int clinicId, int page = 1, int pageSize = 20, string? searchTerm = null, string? actionTypeFilter = null, DateTime? dateFrom = null, DateTime? dateTo = null)
    {
        var auditTrail = await _auditService.GetAuditTrailAsync(clinicId, page, pageSize, searchTerm, actionTypeFilter, dateFrom, dateTo);
        ViewBag.PageTitle = _localizer.T("Page.AuditLog");
        ViewBag.ClinicId = clinicId;

        return View(auditTrail);
    }
}
