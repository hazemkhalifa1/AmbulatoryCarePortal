using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AmbulatoryCarePortal.Application.DTOs;
using AmbulatoryCarePortal.Application.Interfaces;
using AmbulatoryCarePortal.Presentation.Extensions;

namespace AmbulatoryCarePortal.Presentation.Areas.ClinicAdmin.Controllers;

[Area("ClinicAdmin")]
[Authorize(Roles = "HospitalAdmin,ClinicAdmin,DepartmentUser,Auditor,Viewer")]
public class FormsController : Controller
{
    private readonly IFormService _formService;
    private readonly ILogger<FormsController> _logger;

    public FormsController(
        IFormService formService,
        ILogger<FormsController> logger)
    {
        _formService = formService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string searchTerm = "")
    {
        var clinicId = User.GetClinicId() ?? 0;
        var forms = await _formService.GetClinicFormsAsync(clinicId);

        if (!string.IsNullOrEmpty(searchTerm))
        {
            forms = forms.Where(f =>
                f.Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                (f.TitleAr?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false)
            ).ToList();
        }

        ViewBag.SearchTerm = searchTerm;
        ViewBag.PageTitle = "Forms Library";
        return View(forms);
    }

    [HttpGet]
    [Authorize(Roles = "ClinicAdmin,HospitalAdmin")]
    public IActionResult Create()
    {
        ViewBag.PageTitle = "Add Form";
        return View(new CreateFormDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "ClinicAdmin,HospitalAdmin")]
    public async Task<IActionResult> Create(CreateFormDto dto)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.PageTitle = "Add Form";
            return View(dto);
        }

        try
        {
            dto.ClinicId = User.GetClinicId() ?? 0;
            var formId = await _formService.CreateFormAsync(dto);
            TempData["SuccessMessage"] = "Form created successfully";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating form");
            ModelState.AddModelError(string.Empty, "An error occurred while creating the form");
            ViewBag.PageTitle = "Add Form";
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
        ViewBag.PageTitle = $"Versions - {form.Title}";
        return View(versions);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "ClinicAdmin,HospitalAdmin")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _formService.DeleteFormAsync(id);
            TempData["SuccessMessage"] = "Form deleted successfully";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting form");
            TempData["ErrorMessage"] = "An error occurred while deleting the form";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "ClinicAdmin,HospitalAdmin,DepartmentUser")]
    public async Task<IActionResult> UploadVersion(int formId, IFormFile file, string? notes)
    {
        if (file == null || file.Length == 0)
        {
            TempData["ErrorMessage"] = "Please select a file to upload";
            return RedirectToAction(nameof(Versions), new { id = formId });
        }

        var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".xlsx", ".xls" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
        {
            TempData["ErrorMessage"] = "Only PDF, Word, and Excel files are allowed";
            return RedirectToAction(nameof(Versions), new { id = formId });
        }

        if (file.Length > 20 * 1024 * 1024)
        {
            TempData["ErrorMessage"] = "File size must not exceed 20MB";
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

            TempData["SuccessMessage"] = "New version uploaded successfully";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading form version");
            TempData["ErrorMessage"] = "An error occurred while uploading the file";
        }

        return RedirectToAction(nameof(Versions), new { id = formId });
    }
}
