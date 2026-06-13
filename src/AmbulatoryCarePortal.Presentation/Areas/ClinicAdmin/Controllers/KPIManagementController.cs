using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AutoMapper;
using AmbulatoryCarePortal.Application.DTOs;
using AmbulatoryCarePortal.Application.Interfaces;
using AmbulatoryCarePortal.Domain.Entities;
using AmbulatoryCarePortal.Domain.Enums;
using AmbulatoryCarePortal.Presentation.Helpers;
using AmbulatoryCarePortal.Presentation.ViewModels;

namespace AmbulatoryCarePortal.Presentation.Areas.ClinicAdmin.Controllers;

[Area("ClinicAdmin")]
[Authorize(Policy = "Permission.kpis.read")]
public class KPIManagementController : Controller
{
    private readonly IKPIService _kpiService;
    private readonly IAnalyticsService _analyticsService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<KPIManagementController> _logger;
    private readonly ITranslationService _localizer;

    public KPIManagementController(
        IKPIService kpiService,
        IAnalyticsService analyticsService,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<KPIManagementController> logger,
        ITranslationService localizer)
    {
        _kpiService = kpiService;
        _analyticsService = analyticsService;
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
        int? departmentFilter = null)
    {
        var clinicId = int.Parse(User.FindFirst("ClinicId")?.Value ?? "0");

        var kpis = await _unitOfWork.Repository<KPI>().FindAsync(
            k => k.ClinicId == clinicId &&
                 (string.IsNullOrEmpty(searchTerm) || k.Name.Contains(searchTerm)) &&
                 (string.IsNullOrEmpty(frequencyFilter) || k.Frequency.ToString() == frequencyFilter) &&
                 (!departmentFilter.HasValue || k.DepartmentId == departmentFilter.Value)
        );

        var pagedKpis = kpis
            .OrderByDescending(k => k.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var kpiDtos = new List<KPIDetailViewModel>();

        foreach (var kpi in pagedKpis)
        {
            var entries = await _unitOfWork.Repository<KPIEntry>().FindAsync(e => e.KPIId == kpi.Id);
            var latestEntry = entries.OrderByDescending(e => e.PeriodYear)
                .ThenByDescending(e => e.PeriodMonth)
                .FirstOrDefault();

            kpiDtos.Add(new KPIDetailViewModel
            {
                KPI = _mapper.Map<KPIDto>(kpi),
                LatestEntry = _mapper.Map<KPIEntryDto>(latestEntry),
                TotalEntries = entries.Count(),
                AchievementRate = latestEntry != null
                    ? Math.Round((latestEntry.ActualValue / kpi.TargetValue) * 100, 2)
                    : 0
            });
        }

        var departments = await _unitOfWork.Repository<Department>().FindAsync(d => d.ClinicId == clinicId);

        ViewBag.SearchTerm = searchTerm;
        ViewBag.FrequencyFilter = frequencyFilter;
        ViewBag.DepartmentFilter = departmentFilter;
        ViewBag.Departments = departments;
        ViewBag.TotalCount = kpis.Count();
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(kpis.Count() / (double)pageSize);

        return View(kpiDtos);
    }

    [HttpGet]
    public async Task<IActionResult> ByDepartment(int? departmentId)
    {
        var clinicId = int.Parse(User.FindFirst("ClinicId")?.Value ?? "0");
        var departments = await _unitOfWork.Repository<Department>().FindAsync(d => d.ClinicId == clinicId);

        if (departmentId.HasValue)
        {
            var dept = departments.FirstOrDefault(d => d.Id == departmentId.Value);
            if (dept == null)
                return NotFound();

            var kpis = await _unitOfWork.Repository<KPI>().FindAsync(k => k.DepartmentId == departmentId.Value);

            var kpiDtos = new List<KPIDetailViewModel>();
            foreach (var kpi in kpis)
            {
                var entries = await _unitOfWork.Repository<KPIEntry>().FindAsync(e => e.KPIId == kpi.Id);
                var latestEntry = entries.OrderByDescending(e => e.PeriodYear)
                    .ThenByDescending(e => e.PeriodMonth)
                    .FirstOrDefault();

                kpiDtos.Add(new KPIDetailViewModel
                {
                    KPI = _mapper.Map<KPIDto>(kpi),
                    LatestEntry = _mapper.Map<KPIEntryDto>(latestEntry),
                    TotalEntries = entries.Count(),
                    AchievementRate = latestEntry != null
                        ? Math.Round((latestEntry.ActualValue / kpi.TargetValue) * 100, 2)
                        : 0
                });
            }

            ViewBag.DepartmentName = dept.NameEn;
            ViewBag.DepartmentNameAr = dept.NameAr;
            return View(kpiDtos);
        }

        var allDepartmentKpis = new List<DepartmentsKPIGroupViewModel>();
        foreach (var dept in departments.OrderBy(d => d.NameEn))
        {
            var deptKpis = await _unitOfWork.Repository<KPI>().FindAsync(k => k.DepartmentId == dept.Id);
            var deptKpiDtos = new List<KPIDetailViewModel>();
            foreach (var kpi in deptKpis)
            {
                var entries = await _unitOfWork.Repository<KPIEntry>().FindAsync(e => e.KPIId == kpi.Id);
                var latestEntry = entries.OrderByDescending(e => e.PeriodYear)
                    .ThenByDescending(e => e.PeriodMonth)
                    .FirstOrDefault();

                deptKpiDtos.Add(new KPIDetailViewModel
                {
                    KPI = _mapper.Map<KPIDto>(kpi),
                    LatestEntry = _mapper.Map<KPIEntryDto>(latestEntry),
                    TotalEntries = entries.Count(),
                    AchievementRate = latestEntry != null
                        ? Math.Round((latestEntry.ActualValue / kpi.TargetValue) * 100, 2)
                        : 0
                });
            }

            allDepartmentKpis.Add(new DepartmentsKPIGroupViewModel
            {
                DepartmentId = dept.Id,
                DepartmentName = dept.NameEn,
                DepartmentNameAr = dept.NameAr,
                KPIs = deptKpiDtos
            });
        }

        ViewBag.AllDepartments = allDepartmentKpis;
        return View(allDepartmentKpis);
    }

    [HttpGet]
    [Authorize(Policy = "Permission.kpis.create")]
    public async Task<IActionResult> Create()
    {
        var clinicId = int.Parse(User.FindFirst("ClinicId")?.Value ?? "0");
        var departments = await _unitOfWork.Repository<Department>().FindAsync(d => d.ClinicId == clinicId);

        var model = new CreateKPIViewModel
        {
            AvailableDepartments = departments.Select(d => new DepartmentViewModel { Id = d.Id, Name = d.NameEn, ClinicId = d.ClinicId }).ToList(),
            Frequencies = Enum.GetValues(typeof(KPIFrequency))
                .Cast<KPIFrequency>()
                .Select(f => f.ToString())
                .ToList()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "Permission.kpis.create")]
    public async Task<IActionResult> Create(CreateKPIViewModel model)
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

        var kpi = new KPI
        {
            Name = model.Name,
            NameAr = model.NameAr,
            TargetValue = model.TargetValue,
            Frequency = Enum.Parse<KPIFrequency>(model.Frequency),
            DepartmentId = model.DepartmentId,
            ClinicId = clinicIdValue,
            CalculationFormula = model.CalculationFormula,
            EvidenceRequired = model.EvidenceRequired,
            EscalationRule = model.EscalationRule,
            CreatedBy = userId
        };

        await _unitOfWork.Repository<KPI>().AddAsync(kpi);
        await _unitOfWork.SaveChangesAsync();

        var auditLog = new AuditTrail
        {
            ActionType = AuditActionType.Create,
            TargetObjectId = kpi.Id,
            TargetObjectType = nameof(KPI),
            Description = $"Created KPI: {kpi.Name}",
            ClinicId = clinicIdValue,
            CreatedBy = userId,
            ActionDate = DateTime.UtcNow,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        };

        await _unitOfWork.Repository<AuditTrail>().AddAsync(auditLog);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation($"KPI {kpi.Name} created by {userId}");
        TempData["SuccessMessage"] = _localizer.T("Alert.Success.KPICreated");

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> EnterData(int kpiId)
    {
        var kpi = await _unitOfWork.Repository<KPI>().GetByIdAsync(kpiId);
        if (kpi == null)
            return NotFound();

        var clinicId = int.Parse(User.FindFirst("ClinicId")?.Value ?? "0");
        if (kpi.ClinicId != clinicId)
            return Forbid();

        var now = DateTime.UtcNow;
        var model = new KPIEntryViewModel
        {
            KPIId = kpiId,
            KPIName = kpi.Name,
            TargetValue = kpi.TargetValue,
            PeriodMonth = now.Month,
            PeriodYear = now.Year,
            Frequency = kpi.Frequency.ToString()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EnterData(KPIEntryViewModel model, IFormFile evidenceFile)
    {
        var kpi = await _unitOfWork.Repository<KPI>().GetByIdAsync(model.KPIId);
        if (kpi == null)
            return NotFound();

        var clinicId = int.Parse(User.FindFirst("ClinicId")?.Value ?? "0");
        if (kpi.ClinicId != clinicId)
            return Forbid();

        if (!ModelState.IsValid)
        {
            model.KPIName = kpi.Name;
            model.TargetValue = kpi.TargetValue;
            model.Frequency = kpi.Frequency.ToString();
            return View(model);
        }

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        var entry = new KPIEntry
        {
            KPIId = model.KPIId,
            PeriodMonth = model.PeriodMonth,
            PeriodYear = model.PeriodYear,
            ActualValue = model.ActualValue,
            Notes = model.Notes,
            CreatedBy = userId
        };

        if (evidenceFile != null && evidenceFile.Length > 0)
        {
            var fileName = Path.GetRandomFileName() + Path.GetExtension(evidenceFile.FileName);
            var filePath = Path.Combine("wwwroot/uploads/kpi-evidence", fileName);

            Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? "");

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await evidenceFile.CopyToAsync(stream);
            }

            entry.EvidenceFilePath = $"/uploads/kpi-evidence/{fileName}";
        }

        await _unitOfWork.Repository<KPIEntry>().AddAsync(entry);
        await _unitOfWork.SaveChangesAsync();

        var auditLog = new AuditTrail
        {
            ActionType = AuditActionType.Create,
            TargetObjectId = entry.Id,
            TargetObjectType = nameof(KPIEntry),
            Description = $"Entered KPI data for {kpi.Name} - Period {model.PeriodMonth}/{model.PeriodYear}",
            ClinicId = clinicId,
            CreatedBy = userId,
            ActionDate = DateTime.UtcNow,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        };

        await _unitOfWork.Repository<AuditTrail>().AddAsync(auditLog);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation($"KPI data entered for {kpi.Name} by {userId}");
        TempData["SuccessMessage"] = _localizer.T("Alert.Success.KPIDataRecorded");

        return RedirectToAction(nameof(ViewAnalytics), new { id = model.KPIId });
    }

    [HttpGet]
    public async Task<IActionResult> ViewAnalytics(int id)
    {
        var kpi = await _unitOfWork.Repository<KPI>().GetByIdAsync(id);
        if (kpi == null)
            return NotFound();

        var clinicId = int.Parse(User.FindFirst("ClinicId")?.Value ?? "0");
        if (kpi.ClinicId != clinicId)
            return Forbid();

        var entries = (await _unitOfWork.Repository<KPIEntry>().FindAsync(e => e.KPIId == id))
            .OrderByDescending(e => e.PeriodYear)
            .ThenByDescending(e => e.PeriodMonth)
            .ToList();

        var analyticsModel = new KPIAnalyticsViewModel
        {
            KPI = _mapper.Map<KPIDto>(kpi),
            Entries = _mapper.Map<List<KPIEntryDto>>(entries.Take(12).ToList()),
            TotalEntries = entries.Count,
            AverageAchievement = entries.Any()
                ? Math.Round(entries.Average(e => (e.ActualValue / kpi.TargetValue) * 100), 2)
                : 0,
            HighestAchievement = entries.Any()
                ? Math.Round(entries.Max(e => (e.ActualValue / kpi.TargetValue) * 100), 2)
                : 0,
            LowestAchievement = entries.Any()
                ? Math.Round(entries.Min(e => (e.ActualValue / kpi.TargetValue) * 100), 2)
                : 0,
            LastEntryDate = entries.FirstOrDefault()?.CreatedAt ?? DateTime.MinValue,
            Trend = CalculateTrend(entries),
            TrendPercentage = CalculateTrendPercentage(entries)
        };

        return View(analyticsModel);
    }

    [HttpGet]
    public async Task<IActionResult> Compare()
    {
        var clinicId = int.Parse(User.FindFirst("ClinicId")?.Value ?? "0");
        var kpis = await _unitOfWork.Repository<KPI>().FindAsync(k => k.ClinicId == clinicId);

        var comparisonData = new List<KPIComparisonViewModel>();

        foreach (var kpi in kpis)
        {
            var latestEntry = (await _unitOfWork.Repository<KPIEntry>().FindAsync(e => e.KPIId == kpi.Id))
                .OrderByDescending(e => e.PeriodYear)
                .ThenByDescending(e => e.PeriodMonth)
                .FirstOrDefault();

            comparisonData.Add(new KPIComparisonViewModel
            {
                KPIId = kpi.Id,
                KPIName = kpi.Name,
                TargetValue = kpi.TargetValue,
                ActualValue = latestEntry?.ActualValue ?? 0,
                AchievementPercentage = latestEntry != null
                    ? Math.Round((latestEntry.ActualValue / kpi.TargetValue) * 100, 2)
                    : 0,
                Status = latestEntry != null && (latestEntry.ActualValue / kpi.TargetValue) >= 1
                    ? "On Target"
                    : "Below Target"
            });
        }

        return View(comparisonData.OrderByDescending(c => c.AchievementPercentage).ToList());
    }

    [HttpGet]
    public async Task<IActionResult> Export(int kpiId, string format = "excel")
    {
        var kpi = await _unitOfWork.Repository<KPI>().GetByIdAsync(kpiId);
        if (kpi == null)
            return NotFound();

        var entries = await _unitOfWork.Repository<KPIEntry>().FindAsync(e => e.KPIId == kpiId);

        if (format.ToLower() == "pdf")
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes($"KPI: {kpi.Name}, Entries: {entries.Count()}");
            return File(bytes, "application/pdf", $"kpi-{kpi.Name}-export.pdf");
        }
        else
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes($"KPI: {kpi.Name}, Entries: {entries.Count()}");
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"kpi-{kpi.Name}-export.xlsx");
        }
    }

    [HttpGet]
    [Route("api/kpi/summary")]
    public async Task<IActionResult> GetSummary()
    {
        var clinicId = int.Parse(User.FindFirst("ClinicId")?.Value ?? "0");
        var kpis = await _unitOfWork.Repository<KPI>().FindAsync(k => k.ClinicId == clinicId);

        var summary = new
        {
            TotalKPIs = kpis.Count(),
            OnTargetKPIs = 0,
            BelowTargetKPIs = kpis.Count(),
            AverageAchievementRate = 0,
            LastUpdateDate = DateTime.UtcNow
        };

        return Json(summary);
    }

    private string CalculateTrend(List<KPIEntry> entries)
    {
        if (entries.Count < 2)
            return "No trend data";

        var recent = entries.Take(3).Average(e => e.ActualValue);
        var previous = entries.Skip(3).Take(3).Average(e => e.ActualValue);

        if (recent > previous) return "Increasing";
        if (recent < previous) return "Decreasing";
        return "Stable";
    }

    private decimal CalculateTrendPercentage(List<KPIEntry> entries)
    {
        if (entries.Count < 2)
            return 0;

        var latest = entries.FirstOrDefault()?.ActualValue ?? 0;
        var previous = entries.Skip(1).FirstOrDefault()?.ActualValue ?? 0;

        if (previous == 0)
            return 0;

        return Math.Round(((latest - previous) / previous) * 100, 2);
    }
}

public class CreateKPIViewModel
{
    public string Name { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public decimal TargetValue { get; set; }
    public string Frequency { get; set; } = string.Empty;
    public int DepartmentId { get; set; }
    public string? CalculationFormula { get; set; }
    public string? EvidenceRequired { get; set; }
    public string? EscalationRule { get; set; }
    public List<DepartmentViewModel> AvailableDepartments { get; set; } = new();
    public List<string> Frequencies { get; set; } = new();
}

public class KPIEntryViewModel
{
    public int KPIId { get; set; }
    public string KPIName { get; set; } = string.Empty;
    public decimal TargetValue { get; set; }
    public int PeriodMonth { get; set; }
    public int PeriodYear { get; set; }
    public decimal ActualValue { get; set; }
    public string? Notes { get; set; }
    public string Frequency { get; set; } = string.Empty;
}

public class KPIDetailViewModel
{
    public KPIDto KPI { get; set; } = null!;
    public KPIEntryDto? LatestEntry { get; set; }
    public int TotalEntries { get; set; }
    public decimal AchievementRate { get; set; }
}

public class DepartmentsKPIGroupViewModel
{
    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public string? DepartmentNameAr { get; set; }
    public List<KPIDetailViewModel> KPIs { get; set; } = new();
}

public class KPIAnalyticsViewModel
{
    public KPIDto KPI { get; set; } = null!;
    public List<KPIEntryDto> Entries { get; set; } = new();
    public int TotalEntries { get; set; }
    public decimal AverageAchievement { get; set; }
    public decimal HighestAchievement { get; set; }
    public decimal LowestAchievement { get; set; }
    public DateTime LastEntryDate { get; set; }
    public string Trend { get; set; } = string.Empty;
    public decimal TrendPercentage { get; set; }
}

public class KPIComparisonViewModel
{
    public int KPIId { get; set; }
    public string KPIName { get; set; } = string.Empty;
    public decimal TargetValue { get; set; }
    public decimal ActualValue { get; set; }
    public decimal AchievementPercentage { get; set; }
    public string Status { get; set; } = string.Empty;
}
