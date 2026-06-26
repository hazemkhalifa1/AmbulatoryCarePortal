using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AutoMapper;
using AmbulatoryCarePortal.Application.DTOs;
using AmbulatoryCarePortal.Application.Interfaces;
using AmbulatoryCarePortal.Application.Interfaces.Repositories;
using AmbulatoryCarePortal.Domain.Entities;
using AmbulatoryCarePortal.Domain.Enums;
using AmbulatoryCarePortal.Presentation.Helpers;
using AmbulatoryCarePortal.Presentation.ViewModels;
using ChecklistAnswerEntity = AmbulatoryCarePortal.Domain.Entities.ChecklistAnswer;
using ChecklistAnswerEnum = AmbulatoryCarePortal.Domain.Enums.ChecklistAnswer;

namespace AmbulatoryCarePortal.Presentation.Areas.ClinicAdmin.Controllers;

[Area("ClinicAdmin")]
[Authorize(Policy = "Permission.checklists.read")]
public class ChecklistManagementController : Controller
{
    private readonly IChecklistService _checklistService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<ChecklistManagementController> _logger;
    private readonly ITranslationService _localizer;

    public ChecklistManagementController(
        IChecklistService checklistService,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<ChecklistManagementController> logger,
        ITranslationService localizer)
    {
        _checklistService = checklistService;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
        _localizer = localizer;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        int page = 1,
        int pageSize = 10,
        string searchTerm = "",
        string frequencyFilter = "",
        string statusFilter = "")
    {
        var clinicId = int.Parse(User.FindFirst("ClinicId")?.Value ?? "0");

        var templates = await _unitOfWork.Repository<ChecklistTemplate>().FindAsync(
            t => t.ClinicId == clinicId &&
                 (string.IsNullOrEmpty(searchTerm) || t.Name.Contains(searchTerm)) &&
                 (string.IsNullOrEmpty(frequencyFilter) || t.Frequency.ToString() == frequencyFilter)
        );

        var pagedTemplates = templates
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var checklistDtos = new List<ChecklistDetailViewModel>();

        foreach (var template in pagedTemplates)
        {
            var rounds = await _unitOfWork.Repository<ChecklistRound>().FindAsync(
                r => r.ChecklistTemplateId == template.Id
            );

            var completedRounds = rounds.Count(r => r.ExecutedAt != default);
            var completionRate = rounds.Any()
                ? Math.Round((completedRounds * 100m / rounds.Count()), 2)
                : 0;

            checklistDtos.Add(new ChecklistDetailViewModel
            {
                Template = _mapper.Map<ChecklistTemplateDto>(template),
                TotalRounds = rounds.Count(),
                CompletedRounds = completedRounds,
                CompletionRate = completionRate,
                LastExecutedDate = rounds
                    .OrderByDescending(r => r.ExecutedAt)
                    .FirstOrDefault()?.ExecutedAt ?? DateTime.MinValue
            });
        }

        ViewBag.SearchTerm = searchTerm;
        ViewBag.FrequencyFilter = frequencyFilter;
        ViewBag.StatusFilter = statusFilter;
        ViewBag.TotalCount = templates.Count();
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(templates.Count() / (double)pageSize);

        return View(checklistDtos);
    }

    [HttpGet]
    [Authorize(Policy = "Permission.checklists.create")]
    public async Task<IActionResult> Create()
    {
        var clinicId = int.Parse(User.FindFirst("ClinicId")?.Value ?? "0");
        var departments = await _unitOfWork.Repository<Department>().FindAsync(d => d.ClinicId == clinicId);

        var model = new CreateChecklistViewModel
        {
            AvailableDepartments = departments.Select(d => new DepartmentViewModel { Id = d.Id, Name = d.NameEn, ClinicId = d.ClinicId }).ToList(),
            Frequencies = Enum.GetValues(typeof(ChecklistSchedule))
                .Cast<ChecklistSchedule>()
                .Select(f => f.ToString())
                .ToList(),
            Items = new List<ChecklistItemViewModel>()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "Permission.checklists.create")]
    public async Task<IActionResult> Create(CreateChecklistViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var clinicId = int.Parse(User.FindFirst("ClinicId")?.Value ?? "0");
            var departments = await _unitOfWork.Repository<Department>().FindAsync(d => d.ClinicId == clinicId);
            model.AvailableDepartments = departments.Select(d => new DepartmentViewModel { Id = d.Id, Name = d.NameEn, ClinicId = d.ClinicId }).ToList();
            return View(model);
        }

        var clinicIdValue = int.Parse(User.FindFirst("ClinicId")?.Value ?? "0");
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        var template = new ChecklistTemplate
        {
            Name = model.Name,
            NameAr = model.NameAr,
            Frequency = Enum.Parse<ChecklistSchedule>(model.Frequency),
            DepartmentId = model.DepartmentId,
            ClinicId = clinicIdValue,
            Description = model.Description,
            IsActive = true,
            CreatedBy = userId
        };

        await _unitOfWork.Repository<ChecklistTemplate>().AddAsync(template);
        await _unitOfWork.SaveChangesAsync();

        if (model.Items != null && model.Items.Any())
        {
            foreach (var itemModel in model.Items)
            {
                var item = new ChecklistItem
                {
                    ChecklistTemplateId = template.Id,
                    QuestionText = itemModel.QuestionText,
                    QuestionTextAr = itemModel.QuestionTextAr,
                    ItemOrder = itemModel.ItemOrder,
                    Weight = itemModel.Weight ?? 1,
                    CreatedBy = userId
                };

                await _unitOfWork.Repository<ChecklistItem>().AddAsync(item);
            }

            await _unitOfWork.SaveChangesAsync();
        }

        var auditLog = new AuditTrail
        {
            ActionType = AuditActionType.Create,
            TargetObjectId = template.Id,
            TargetObjectType = nameof(ChecklistTemplate),
            Description = $"Created checklist template: {template.Name}",
            ClinicId = clinicIdValue,
            CreatedBy = userId,
            ActionDate = DateTime.UtcNow,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        };

        await _unitOfWork.Repository<AuditTrail>().AddAsync(auditLog);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Checklist template {Name} created by {UserId}", template.Name, userId);
        TempData["SuccessMessage"] = _localizer.T("Alert.Success.ChecklistCreated");

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Policy = "Permission.checklists.execute")]
    public async Task<IActionResult> Execute(int templateId)
    {
        var template = await _unitOfWork.Repository<ChecklistTemplate>().GetByIdAsync(templateId);
        if (template == null)
            return NotFound();

        var clinicId = int.Parse(User.FindFirst("ClinicId")?.Value ?? "0");
        if (template.ClinicId != clinicId)
            return Forbid();

        var items = await _unitOfWork.Repository<ChecklistItem>().FindAsync(
            i => i.ChecklistTemplateId == templateId
        );

        var model = new ExecuteChecklistViewModel
        {
            TemplateId = templateId,
            TemplateName = template.Name,
            Items = items.OrderBy(i => i.ItemOrder).Select(i => new ChecklistItemAnswerViewModel
            {
                ItemId = i.Id,
                QuestionText = i.QuestionText,
                QuestionTextAr = i.QuestionTextAr,
                ItemOrder = i.ItemOrder,
                Answer = ChecklistAnswerEnum.Yes
            }).ToList(),
            ExecutedDate = DateTime.UtcNow
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "Permission.checklists.execute")]
    public async Task<IActionResult> Execute(int templateId, ExecuteChecklistViewModel model, IFormFile evidenceFile)
    {
        var template = await _unitOfWork.Repository<ChecklistTemplate>().GetByIdAsync(templateId);
        if (template == null)
            return NotFound();

        var clinicId = int.Parse(User.FindFirst("ClinicId")?.Value ?? "0");
        if (template.ClinicId != clinicId)
            return Forbid();

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        var round = new ChecklistRound
        {
            ChecklistTemplateId = templateId,
            ClinicId = clinicId,
            ExecutedAt = DateTime.UtcNow,
            ExecutedByUserId = userId,
            Notes = model.Notes,
            CreatedBy = userId
        };

        await _unitOfWork.Repository<ChecklistRound>().AddAsync(round);
        await _unitOfWork.SaveChangesAsync();

        if (model.Items != null && model.Items.Any())
        {
            foreach (var itemAnswer in model.Items)
            {
                var answer = new ChecklistAnswerEntity
                {
                    ChecklistRoundId = round.Id,
                    ChecklistItemId = itemAnswer.ItemId,
                    AnswerValue = itemAnswer.Answer,
                    Notes = itemAnswer.Notes,
                    CreatedBy = userId
                };

                await _unitOfWork.Repository<ChecklistAnswerEntity>().AddAsync(answer);
            }

            await _unitOfWork.SaveChangesAsync();
        }

        if (evidenceFile != null)
        {
            var (isValid, errorMsg) = FileUploadValidator.ValidateDocument(evidenceFile);
            if (!isValid)
            {
                TempData["ErrorMessage"] = errorMsg;
                return RedirectToAction(nameof(ViewHistory), new { templateId });
            }
        }

        if (evidenceFile != null && evidenceFile.Length > 0)
        {
            var fileName = Path.GetRandomFileName() + Path.GetExtension(evidenceFile.FileName);
            var filePath = Path.Combine("wwwroot/uploads/checklist-evidence", fileName);

            Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? "");

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await evidenceFile.CopyToAsync(stream);
            }

            round.EvidenceFilePath = $"/uploads/checklist-evidence/{fileName}";
            _unitOfWork.Repository<ChecklistRound>().Update(round);
            await _unitOfWork.SaveChangesAsync();
        }

        var auditLog = new AuditTrail
        {
            ActionType = AuditActionType.Create,
            TargetObjectId = round.Id,
            TargetObjectType = nameof(ChecklistRound),
            Description = $"Executed checklist: {template.Name}",
            ClinicId = clinicId,
            CreatedBy = userId,
            ActionDate = DateTime.UtcNow,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        };

        await _unitOfWork.Repository<AuditTrail>().AddAsync(auditLog);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Checklist {Name} executed by {UserId}", template.Name, userId);
        TempData["SuccessMessage"] = _localizer.T("Alert.Success.ChecklistExecuted");

        return RedirectToAction(nameof(ViewHistory), new { templateId });
    }

    [HttpGet]
    public async Task<IActionResult> ViewHistory(int templateId)
    {
        var template = await _unitOfWork.Repository<ChecklistTemplate>().GetByIdAsync(templateId);
        if (template == null)
            return NotFound();

        var clinicId = int.Parse(User.FindFirst("ClinicId")?.Value ?? "0");
        if (template.ClinicId != clinicId)
            return Forbid();

        var rounds = (await _unitOfWork.Repository<ChecklistRound>().FindAsync(
            r => r.ChecklistTemplateId == templateId
        )).OrderByDescending(r => r.ExecutedAt).ToList();

        var historyDtos = new List<ChecklistHistoryViewModel>();

        foreach (var round in rounds)
        {
            var answers = await _unitOfWork.Repository<ChecklistAnswerEntity>().FindAsync(
                a => a.ChecklistRoundId == round.Id
            );

            var yesCount = answers.Count(a => a.AnswerValue == ChecklistAnswerEnum.Yes);
            var totalCount = answers.Count();
            var completionRate = totalCount > 0 ? Math.Round((yesCount * 100m / totalCount), 2) : 0;

            historyDtos.Add(new ChecklistHistoryViewModel
            {
                Round = _mapper.Map<ChecklistRoundDto>(round),
                TotalItems = totalCount,
                CompletedItems = yesCount,
                CompletionRate = completionRate
            });
        }

        ViewBag.TemplateName = template.Name;
        ViewBag.TemplateId = templateId;

        return View(historyDtos);
    }

    [HttpPost]
    [Authorize(Policy = "Permission.checklists.approve")]
    public async Task<IActionResult> Approve(int roundId)
    {
        var round = await _unitOfWork.Repository<ChecklistRound>().GetByIdAsync(roundId);
        if (round == null)
            return NotFound();

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        round.ApprovedAt = DateTime.UtcNow;
        round.ApprovedByUserId = userId;

        _unitOfWork.Repository<ChecklistRound>().Update(round);
        await _unitOfWork.SaveChangesAsync();

        var auditLog = new AuditTrail
        {
            ActionType = AuditActionType.Approve,
            TargetObjectId = round.Id,
            TargetObjectType = nameof(ChecklistRound),
            Description = "Approved checklist round",
            ClinicId = round.ClinicId,
            CreatedBy = userId,
            ActionDate = DateTime.UtcNow,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        };

        await _unitOfWork.Repository<AuditTrail>().AddAsync(auditLog);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Checklist round {RoundId} approved by {UserId}", roundId, userId);
        TempData["SuccessMessage"] = _localizer.T("Alert.Success.ChecklistApproved");

        return RedirectToAction(nameof(ViewHistory), new { templateId = round.ChecklistTemplateId });
    }

    [HttpGet]
    [Route("api/checklist/analytics")]
    public async Task<IActionResult> GetAnalytics(int templateId)
    {
        var template = await _unitOfWork.Repository<ChecklistTemplate>().GetByIdAsync(templateId);
        if (template == null)
            return NotFound();

        var rounds = await _unitOfWork.Repository<ChecklistRound>().FindAsync(
            r => r.ChecklistTemplateId == templateId
        );

        var analytics = new
        {
            TemplateName = template.Name,
            TotalRounds = rounds.Count(),
            CompletedRounds = rounds.Count(r => r.ExecutedAt != default),
            ApprovedRounds = rounds.Count(r => r.ApprovedAt.HasValue),
            CompletionRate = rounds.Any() ? Math.Round((rounds.Count(r => r.ExecutedAt != default) * 100m / rounds.Count()), 2) : 0,
            ApprovalRate = rounds.Count(r => r.ExecutedAt != default) > 0
                ? Math.Round((rounds.Count(r => r.ApprovedAt.HasValue) * 100m / rounds.Count(r => r.ExecutedAt != default)), 2)
                : 0,
            LastExecutedDate = rounds.OrderByDescending(r => r.ExecutedAt).FirstOrDefault()?.ExecutedAt ?? DateTime.MinValue
        };

        return Json(analytics);
    }

    [HttpGet]
    public async Task<IActionResult> Export(int templateId, string format = "excel")
    {
        var template = await _unitOfWork.Repository<ChecklistTemplate>().GetByIdAsync(templateId);
        if (template == null)
            return NotFound();

        var rounds = await _unitOfWork.Repository<ChecklistRound>().FindAsync(
            r => r.ChecklistTemplateId == templateId
        );

        if (format.ToLower() == "pdf")
        {
            var pdfContent = $"Checklist: {template.Name}, Rounds: {rounds.Count()}";
            var bytes = System.Text.Encoding.UTF8.GetBytes(pdfContent);
            return File(bytes, "application/pdf", $"checklist-{template.Name}-export.pdf");
        }
        else
        {
            var excelContent = $"Checklist: {template.Name}, Rounds: {rounds.Count()}";
            var bytes = System.Text.Encoding.UTF8.GetBytes(excelContent);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"checklist-{template.Name}-export.xlsx");
        }
    }
}

public class CreateChecklistViewModel
{
    public string Name { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public string Frequency { get; set; } = string.Empty;
    public int? DepartmentId { get; set; }
    public string? Description { get; set; }
    public List<ChecklistItemViewModel> Items { get; set; } = new();
    public List<DepartmentViewModel> AvailableDepartments { get; set; } = new();
    public List<string> Frequencies { get; set; } = new();
}

public class ExecuteChecklistViewModel
{
    public int TemplateId { get; set; }
    public string TemplateName { get; set; } = string.Empty;
    public List<ChecklistItemAnswerViewModel> Items { get; set; } = new();
    public string? Notes { get; set; }
    public DateTime ExecutedDate { get; set; }
}

public class ChecklistItemViewModel
{
    public int Id { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string? QuestionTextAr { get; set; }
    public int ItemOrder { get; set; }
    public int? Weight { get; set; }
}

public class ChecklistItemAnswerViewModel
{
    public int ItemId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string? QuestionTextAr { get; set; }
    public int ItemOrder { get; set; }
    public Domain.Enums.ChecklistAnswer Answer { get; set; }
    public string? Notes { get; set; }
}

public class ChecklistDetailViewModel
{
    public ChecklistTemplateDto Template { get; set; } = null!;
    public int TotalRounds { get; set; }
    public int CompletedRounds { get; set; }
    public decimal CompletionRate { get; set; }
    public DateTime LastExecutedDate { get; set; }
}

public class ChecklistHistoryViewModel
{
    public ChecklistRoundDto Round { get; set; } = null!;
    public int TotalItems { get; set; }
    public int CompletedItems { get; set; }
    public decimal CompletionRate { get; set; }
}
