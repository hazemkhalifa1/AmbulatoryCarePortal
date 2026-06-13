using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AmbulatoryCarePortal.Application.Common;
using AmbulatoryCarePortal.Application.Constants;
using AmbulatoryCarePortal.Application.DTOs.PolicyDocument;
using AmbulatoryCarePortal.Application.Interfaces;
using AmbulatoryCarePortal.Domain.Entities;
using AmbulatoryCarePortal.Domain.Enums;
using AmbulatoryCarePortal.Presentation.Helpers;

namespace AmbulatoryCarePortal.Presentation.Areas.ClinicAdmin.Controllers;

[Area("ClinicAdmin")]
[Authorize(Policy = "Permission.policies.read")]
public class PolicyDocumentsController : Controller
{
    private readonly IPolicyDocumentService _policyService;
    private readonly IClinicDocumentService _clinicDocumentService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PolicyDocumentsController> _logger;
    private readonly ITranslationService _localizer;

    public PolicyDocumentsController(
        IPolicyDocumentService policyService,
        IClinicDocumentService clinicDocumentService,
        IUnitOfWork unitOfWork,
        ILogger<PolicyDocumentsController> logger,
        ITranslationService localizer)
    {
        _policyService = policyService;
        _clinicDocumentService = clinicDocumentService;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _localizer = localizer;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? searchTerm = null, string? statusFilter = null, string? standardFilter = null)
    {
        var clinicId = User.FindFirst("ClinicId") != null
            ? int.Parse(User.FindFirst("ClinicId")?.Value ?? "0")
            : 0;

        var clinic = await _unitOfWork.Repository<Clinic>().GetByIdAsync(clinicId);
        string[]? standards = null;

        if (clinic != null)
        {
            standards = ClinicTypeStandards.GetStandards(clinic.ClinicType);
        }

        var policies = await _policyService.GetClinicPoliciesAsync(clinicId, 1, int.MaxValue);
        var clinicDocuments = await _clinicDocumentService.GetClinicDocumentsAsync(clinicId, searchTerm, statusFilter, standardFilter);

        var policyItems = policies.Data.Select(p => new PolicyDocumentListItem
        {
            Id = p.Id,
            Title = p.Title,
            TitleAr = p.TitleAr,
            StandardCode = p.StandardCode,
            DepartmentName = p.DepartmentName,
            DocumentStatus = p.DocumentStatus.ToString(),
            ExpiryDate = p.ExpiryDate,
            VersionNumber = p.VersionNumber,
            AttachmentCount = p.AttachmentCount,
            Type = "Policy"
        });

        var clinicDocItems = clinicDocuments.Select(d => new PolicyDocumentListItem
        {
            Id = d.Id,
            Title = d.TitleEn,
            TitleAr = d.TitleAr,
            StandardCode = d.StandardCode,
            DepartmentName = d.DepartmentCategory ?? "",
            DocumentStatus = d.DocumentStatus.ToString(),
            ExpiryDate = d.ExpiryDate,
            VersionNumber = 1,
            AttachmentCount = d.AttachmentCount,
            Type = "ClinicDocument"
        });

        var allItems = policyItems.Concat(clinicDocItems).ToList();

        if (!string.IsNullOrEmpty(searchTerm))
        {
            allItems = allItems.Where(i =>
                i.Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                (i.TitleAr != null && i.TitleAr.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
            ).ToList();
        }

        if (!string.IsNullOrEmpty(standardFilter))
        {
            allItems = allItems.Where(i =>
                i.StandardCode != null && i.StandardCode.Equals(standardFilter, StringComparison.OrdinalIgnoreCase)
            ).ToList();
        }

        if (!string.IsNullOrEmpty(statusFilter))
        {
            allItems = allItems.Where(i =>
                i.DocumentStatus.Equals(statusFilter, StringComparison.OrdinalIgnoreCase)
            ).ToList();
        }

        ViewBag.SearchTerm = searchTerm;
        ViewBag.StatusFilter = statusFilter;
        ViewBag.StandardFilter = standardFilter;
        ViewBag.Standards = standards;
        ViewBag.TotalCount = allItems.Count;

        return View(allItems);
    }

    [HttpGet]
    public IActionResult Details(int id, string type = "Policy")
    {
        if (type == "ClinicDocument")
        {
            return RedirectToAction("Details", "ClinicDocuments", new { area = "ClinicAdmin", id });
        }

        return RedirectToAction("Details", "PolicyManagement", new { area = "ClinicAdmin", id });
    }
}

public class PolicyDocumentListItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? TitleAr { get; set; }
    public string? StandardCode { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public string DocumentStatus { get; set; } = string.Empty;
    public DateTime? ExpiryDate { get; set; }
    public int VersionNumber { get; set; }
    public int AttachmentCount { get; set; }
    public string Type { get; set; } = "Policy";
}
