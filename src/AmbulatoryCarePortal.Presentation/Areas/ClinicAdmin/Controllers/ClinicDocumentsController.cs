using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AmbulatoryCarePortal.Application.Constants;
using AmbulatoryCarePortal.Application.DTOs.Document;
using AmbulatoryCarePortal.Application.Interfaces;
using AmbulatoryCarePortal.Domain.Entities;
using AmbulatoryCarePortal.Domain.Enums;
using AmbulatoryCarePortal.Presentation.Helpers;

namespace AmbulatoryCarePortal.Presentation.Areas.ClinicAdmin.Controllers;

[Area("ClinicAdmin")]
[Authorize(Policy = "Permission.documents.manage")]
public class ClinicDocumentsController : Controller
{
    private readonly IClinicTemplateAssignmentService _assignmentService;
    private readonly IDocumentGenerationService _generationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ClinicDocumentsController> _logger;
    private readonly ITranslationService _localizer;

    public ClinicDocumentsController(
        IClinicTemplateAssignmentService assignmentService,
        IDocumentGenerationService generationService,
        IUnitOfWork unitOfWork,
        ILogger<ClinicDocumentsController> logger,
        ITranslationService localizer)
    {
        _assignmentService = assignmentService;
        _generationService = generationService;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _localizer = localizer;
    }

    private int GetCurrentClinicId()
    {
        var claim = User.FindFirst("ClinicId");
        if (claim != null && int.TryParse(claim.Value, out var id))
            return id;
        return 0;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? searchTerm = null, string? statusFilter = null, string? standardFilter = null)
    {
        var clinicId = GetCurrentClinicId();

        var clinic = await _unitOfWork.Repository<Clinic>().GetByIdAsync(clinicId);
        string[]? standards = null;

        if (clinic != null)
        {
            standards = ClinicTypeStandards.GetStandards(clinic.ClinicType);
        }

        var assignments = await _assignmentService.GetAssignmentsByClinicAsync(clinicId, searchTerm, statusFilter);

        if (!string.IsNullOrEmpty(standardFilter) && assignments.Any())
        {
            assignments = assignments.Where(a =>
                a.StandardCode.StartsWith(standardFilter, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        ViewBag.SearchTerm = searchTerm;
        ViewBag.StatusFilter = statusFilter;
        ViewBag.StandardFilter = standardFilter;
        ViewBag.Standards = standards;

        return View(assignments);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var clinicId = GetCurrentClinicId();

        var assignment = await _assignmentService.GetAssignmentByIdAsync(id);
        if (assignment == null)
            return NotFound();

        if (assignment.ClinicId != clinicId)
            return Forbid();

        var variableValues = await _assignmentService.GetValuesForAssignmentAsync(id);
        var generatedDocs = await _generationService.GetGeneratedDocumentsAsync(id);

        ViewBag.VariableValues = variableValues;
        ViewBag.GeneratedDocuments = generatedDocs;

        return View(assignment);
    }

    [HttpGet]
    public async Task<IActionResult> Download(int id)
    {
        var clinicId = GetCurrentClinicId();

        var assignment = await _assignmentService.GetAssignmentByIdAsync(id);
        if (assignment == null || assignment.ClinicId != clinicId)
            return Forbid();

        var fileBytes = await _generationService.PreviewDocxAsync(id);
        if (fileBytes == null)
        {
            TempData["ErrorMessage"] = _localizer.T("Alert.Error.TemplateFileNotFound");
            return RedirectToAction(nameof(Index));
        }

        var clinic = await _unitOfWork.Repository<Clinic>().GetByIdAsync(clinicId);
        var clinicName = clinic?.Name ?? "Clinic";
        var fileName = $"{clinicName}_{assignment.StandardCode}.docx";

        return File(fileBytes,
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            fileName);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, ClinicDocumentStatus status)
    {
        var clinicId = GetCurrentClinicId();
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        var assignment = await _assignmentService.GetAssignmentByIdAsync(id);
        if (assignment == null || assignment.ClinicId != clinicId)
            return Forbid();

        var updated = await _assignmentService.UpdateAssignmentStatusAsync(id, status.ToString());
        if (!updated)
        {
            TempData["ErrorMessage"] = _localizer.T("Alert.Error.StatusUpdateFailed");
            return RedirectToAction(nameof(Details), new { id });
        }

        var auditLog = new AuditTrail
        {
            ActionType = AuditActionType.Update,
            TargetObjectId = id,
            TargetObjectType = nameof(ClinicTemplateAssignment),
            Description = $"Updated assignment status to {status}",
            ClinicId = clinicId,
            CreatedBy = userId,
            ActionDate = DateTime.UtcNow,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        };

        await _unitOfWork.Repository<AuditTrail>().AddAsync(auditLog);
        await _unitOfWork.SaveChangesAsync();

        TempData["SuccessMessage"] = _localizer.T("Alert.Info.StatusUpdated", status);
        return RedirectToAction(nameof(Details), new { id });
    }
}
