using AmbulatoryCarePortal.Application.DTOs;
using AmbulatoryCarePortal.Application.Interfaces;
using AmbulatoryCarePortal.Domain.Entities;
using AmbulatoryCarePortal.Presentation.Extensions;
using AmbulatoryCarePortal.Presentation.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AmbulatoryCarePortal.Presentation.Areas.ClinicAdmin.Controllers;

[Area("ClinicAdmin")]
[Authorize(Policy = "Permission.documents.manage")]
public class FormsController : Controller
{
    private readonly IFormService _formService;
    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger<FormsController> _logger;
    private readonly ITranslationService _localizer;

    public FormsController(
        IFormService formService,
        ILogger<FormsController> logger,
        UserManager<AppUser> userManager,
        ITranslationService localizer)
    {
        _formService = formService;
        _logger = logger;
        _userManager = userManager;
        _localizer = localizer;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string searchTerm = "")
    {
        var user = await _userManager.GetUserAsync(User);
        var clinicId = user?.ClinicId ?? 0;
        var forms = await _formService.GetClinicFormsAsync(clinicId);

        if (!string.IsNullOrEmpty(searchTerm))
        {
            forms = forms.Where(f =>
                f.Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                (f.TitleAr?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false)
            ).ToList();
        }

        ViewBag.SearchTerm = searchTerm;
        ViewBag.PageTitle = _localizer.T("Page.FormsLibrary");
        return View(forms);
    }

    [HttpGet]
    [Authorize(Policy = "Permission.documents.manage")]
    public IActionResult Create()
    {
        ViewBag.PageTitle = _localizer.T("Page.AddForm");
        return View(new CreateFormDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "Permission.documents.manage")]
    public async Task<IActionResult> Create(CreateFormDto dto)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.PageTitle = _localizer.T("Page.AddForm");
            return View(dto);
        }

        try
        {
            var user = await _userManager.GetUserAsync(User);
            dto.ClinicId = user?.ClinicId ?? 0;
            var formId = await _formService.CreateFormAsync(dto);
            TempData["SuccessMessage"] = _localizer.T("Alert.Success.FormCreated");
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating form");
            ModelState.AddModelError(string.Empty, "An error occurred while creating the form");
            ViewBag.PageTitle = _localizer.T("Page.AddForm");
            return View(dto);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Versions(int id)
    {
        var form = await _formService.GetFormByIdAsync(id);
        if (form == null)
            return NotFound();

        var versions = await _formService.GetFormVersionHistoryAsync(id);
        ViewBag.Form = form;
        ViewBag.PageTitle = _localizer.T("Page.Versions", form.Title);
        return View(versions);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "Permission.documents.manage")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _formService.DeleteFormAsync(id);
            TempData["SuccessMessage"] = _localizer.T("Alert.Success.FormDeleted");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting form");
            TempData["ErrorMessage"] = _localizer.T("Alert.Error.FormDeleteFailed");
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "Permission.documents.manage")]
    public async Task<IActionResult> UploadVersion(int formId, IFormFile file, string? notes)
    {
        if (file == null || file.Length == 0)
        {
            TempData["ErrorMessage"] = _localizer.T("Alert.Error.NoFileSelected");
            return RedirectToAction(nameof(Versions), new { id = formId });
        }

        var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".xlsx", ".xls" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
        {
            TempData["ErrorMessage"] = _localizer.T("Alert.Error.InvalidFileType");
            return RedirectToAction(nameof(Versions), new { id = formId });
        }

        if (file.Length > 20 * 1024 * 1024)
        {
            TempData["ErrorMessage"] = _localizer.T("Alert.Error.FileTooLarge");
            return RedirectToAction(nameof(Versions), new { id = formId });
        }

        try
        {
            var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "forms", formId.ToString());
            Directory.CreateDirectory(uploadsDir);

            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsDir, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var relativePath = $"/uploads/forms/{formId}/{fileName}";
            var userId = User.GetUserId();
            await _formService.UploadNewVersionAsync(formId, relativePath, userId, notes);

            TempData["SuccessMessage"] = _localizer.T("Alert.Success.FileUploaded");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading form version");
            TempData["ErrorMessage"] = _localizer.T("Alert.Error.FileUploadFailed");
        }

        return RedirectToAction(nameof(Versions), new { id = formId });
    }
}
