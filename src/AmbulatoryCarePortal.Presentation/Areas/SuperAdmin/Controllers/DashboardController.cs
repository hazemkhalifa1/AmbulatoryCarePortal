using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AmbulatoryCarePortal.Application.Interfaces;
using AmbulatoryCarePortal.Domain.Enums;
using AmbulatoryCarePortal.Domain.Entities;
using AmbulatoryCarePortal.Presentation.ViewModels;
using AmbulatoryCarePortal.Presentation.Helpers;

namespace AmbulatoryCarePortal.Presentation.Areas.SuperAdmin.Controllers;

[Area("SuperAdmin")]
[Authorize(Policy = "Permission.system.configure")]
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

        // Aggregate metrics across all clinics for dashboard KPI cards
        var allClinics = await _unitOfWork.Repository<Clinic>().GetAllAsync();
        var allPolicies = await _unitOfWork.Repository<PolicyDocument>().GetAllAsync();

        var avgCompliance = allClinics.Any()
            ? Math.Round(allClinics.Average(c => c.ComplianceScore), 1)
            : 0m;

        var pendingReviews = allPolicies.Count(p =>
            p.DocumentStatus == DocumentStatus.NeedsReview ||
            p.DocumentStatus == DocumentStatus.Pending);

        var overdueItems = allPolicies.Count(p =>
            p.DocumentStatus == DocumentStatus.Expired);

        var viewModel = new SuperAdminDashboardViewModel
        {
            Metrics = new DashboardMetricsViewModel
            {
                TotalClinics = allClinics.Count(),
                AverageCompliance = avgCompliance,
                PendingApprovals = pendingReviews,
                OverdueItems = overdueItems
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
        return View(new CreateClinicViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateClinic(CreateClinicViewModel model, IFormFile? logoFile)
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

            if (logoFile != null && logoFile.Length > 0)
            {
                var uploadsDir = Path.Combine("wwwroot", "uploads", "clinic-logos");
                Directory.CreateDirectory(uploadsDir);
                var ext = Path.GetExtension(logoFile.FileName);
                var fileName = $"clinic_{clinicId}_{Path.GetRandomFileName()}{ext}";
                var filePath = Path.Combine(uploadsDir, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await logoFile.CopyToAsync(stream);
                }
                var clinic = await _unitOfWork.Repository<Clinic>().GetByIdAsync(clinicId);
                if (clinic != null)
                {
                    clinic.LogoPath = $"/uploads/clinic-logos/{fileName}";
                }
            }

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var auditLog = new AuditTrail
            {
                ClinicId = clinicId,
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

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var clinic = await _clinicService.GetClinicDetailsAsync(id);
        if (clinic == null)
            return NotFound();

        var model = new CreateClinicViewModel
        {
            Id = clinic.Id,
            Name = clinic.Name,
            NameAr = clinic.NameAr,
            CityEn = clinic.CityEn,
            CityAr = clinic.CityAr,
            ClinicType = clinic.ClinicType,
            LicenseNumber = clinic.LicenseNumber,
            LicenseExpiry = clinic.LicenseExpiry,
            IsActive = clinic.IsActive,
            ExistingLogoPath = clinic.LogoPath
        };

        ViewBag.PageTitle = _localizer.T("Page.EditClinic");
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CreateClinicViewModel model, IFormFile? logoFile)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.PageTitle = _localizer.T("Page.EditClinic");
            return View(model);
        }

        try
        {
            var clinic = await _unitOfWork.Repository<Clinic>().GetByIdAsync(id);
            if (clinic == null)
                return NotFound();

            clinic.Name = model.Name;
            clinic.NameAr = model.NameAr;
            clinic.CityEn = model.CityEn;
            clinic.CityAr = model.CityAr;
            clinic.ClinicType = model.ClinicType;
            clinic.LicenseNumber = model.LicenseNumber;
            clinic.LicenseExpiry = model.LicenseExpiry;
            clinic.IsActive = model.IsActive;

            if (logoFile != null && logoFile.Length > 0)
            {
                // Delete old logo if exists
                if (!string.IsNullOrEmpty(clinic.LogoPath))
                {
                    var oldPath = Path.Combine("wwwroot", clinic.LogoPath.TrimStart('/'));
                    if (System.IO.File.Exists(oldPath))
                        System.IO.File.Delete(oldPath);
                }

                var uploadsDir = Path.Combine("wwwroot", "uploads", "clinic-logos");
                Directory.CreateDirectory(uploadsDir);
                var ext = Path.GetExtension(logoFile.FileName);
                var fileName = $"clinic_{id}_{Path.GetRandomFileName()}{ext}";
                var filePath = Path.Combine(uploadsDir, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await logoFile.CopyToAsync(stream);
                }
                clinic.LogoPath = $"/uploads/clinic-logos/{fileName}";
            }

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var auditLog = new AuditTrail
            {
                ClinicId = id,
                ActionType = AuditActionType.Update,
                TargetObjectId = id,
                TargetObjectType = nameof(Clinic),
                Description = $"Edited clinic: {model.Name}",
                CreatedBy = userId,
                ActionDate = DateTime.UtcNow,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            };

            await _unitOfWork.Repository<AuditTrail>().AddAsync(auditLog);
            await _unitOfWork.SaveChangesAsync();

            TempData["SuccessMessage"] = _localizer.T("Alert.Success.ClinicUpdated");

            return RedirectToAction("ClinicDetail", new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error editing clinic {ClinicId}", id);
            ModelState.AddModelError(string.Empty, _localizer.T("Alert.Error.ClinicEditFailed"));
            ViewBag.PageTitle = _localizer.T("Page.EditClinic");
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var clinic = await _unitOfWork.Repository<Clinic>().GetByIdAsync(id);
        if (clinic == null)
            return NotFound();

        try
        {
            // Delete logo file if exists
            if (!string.IsNullOrEmpty(clinic.LogoPath))
            {
                var logoPath = Path.Combine("wwwroot", clinic.LogoPath.TrimStart('/'));
                if (System.IO.File.Exists(logoPath))
                    System.IO.File.Delete(logoPath);
            }

            var clinicName = clinic.Name;

            await _clinicService.DeleteClinicAsync(id);

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var auditLog = new AuditTrail
            {
                ClinicId = id,
                ActionType = AuditActionType.Delete,
                TargetObjectId = id,
                TargetObjectType = nameof(Clinic),
                Description = $"Deleted clinic: {clinicName}",
                CreatedBy = userId,
                ActionDate = DateTime.UtcNow,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            };

            await _unitOfWork.Repository<AuditTrail>().AddAsync(auditLog);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Deleted clinic {ClinicId}: {Name}", id, clinicName);
            TempData["SuccessMessage"] = _localizer.T("Alert.Success.ClinicDeleted");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting clinic {ClinicId}", id);
            TempData["ErrorMessage"] = _localizer.T("Alert.Error.ClinicDeleteFailed");
        }

        return RedirectToAction(nameof(Clinics));
    }

    public async Task<IActionResult> AuditLog(int clinicId, int page = 1, int pageSize = 20, string? searchTerm = null, string? actionTypeFilter = null, DateTime? dateFrom = null, DateTime? dateTo = null)
    {
        var auditTrail = await _auditService.GetAuditTrailAsync(clinicId, page, pageSize, searchTerm, actionTypeFilter, dateFrom, dateTo);
        ViewBag.PageTitle = _localizer.T("Page.AuditLog");
        ViewBag.ClinicId = clinicId;

        return View(auditTrail);
    }
}
