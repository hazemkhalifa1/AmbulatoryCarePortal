using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AmbulatoryCarePortal.Application.Interfaces;
using AmbulatoryCarePortal.Presentation.ViewModels;

namespace AmbulatoryCarePortal.Presentation.Areas.SuperAdmin.Controllers;

[Area("SuperAdmin")]
[Authorize(Roles = "SuperAdmin")]
public class DashboardController : Controller
{
    private readonly IClinicService _clinicService;
    private readonly IAuditService _auditService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        IClinicService clinicService,
        IAuditService auditService,
        ILogger<DashboardController> logger)
    {
        _clinicService = clinicService;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var clinics = await _clinicService.GetAllClinicsAsync(1, 10);
        ViewBag.TotalClinics = clinics.TotalCount;
        ViewBag.PageTitle = "Dashboard";
        
        return View(clinics.Data);
    }

    public async Task<IActionResult> Clinics(int page = 1, int pageSize = 10)
    {
        var clinics = await _clinicService.GetAllClinicsAsync(page, pageSize);
        ViewBag.PageTitle = "Manage Clinics";
        
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
        ViewBag.PageTitle = "Create Clinic";
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateClinic(CreateClinicViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.PageTitle = "Create Clinic";
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
            TempData["SuccessMessage"] = "Clinic created successfully";

            return RedirectToAction("ClinicDetail", new { id = clinicId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating clinic");
            ModelState.AddModelError(string.Empty, "An error occurred while creating the clinic");
            ViewBag.PageTitle = "Create Clinic";
            return View(model);
        }
    }

    public async Task<IActionResult> AuditLog(int clinicId, int pageSize = 50)
    {
        var auditTrail = await _auditService.GetAuditTrailAsync(clinicId, pageSize);
        ViewBag.PageTitle = "Audit Log";
        ViewBag.ClinicId = clinicId;
        
        return View(auditTrail);
    }
}
