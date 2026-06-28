using AmbulatoryCarePortal.Application.DTOs;
using AmbulatoryCarePortal.Application.Interfaces;
using AmbulatoryCarePortal.Domain.Entities;
using AmbulatoryCarePortal.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace AmbulatoryCarePortal.Application.Services;

public class ComplianceCalendarService : IComplianceCalendarService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ComplianceCalendarService> _logger;

    public ComplianceCalendarService(
        IUnitOfWork unitOfWork,
        ILogger<ComplianceCalendarService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ComplianceCalendarViewModel> GetCalendarAsync(int clinicId)
    {
        var items = await GetAllItemsAsync(clinicId);
        var today = DateTime.UtcNow.Date;
        var weekEnd = today.AddDays(7);

        var viewModel = new ComplianceCalendarViewModel
        {
            Items = items.OrderBy(i => i.DaysRemaining).ToList(),
            CriticalCount = items.Count(i => i.Severity == ComplianceItemSeverity.Critical),
            WarningCount = items.Count(i => i.Severity == ComplianceItemSeverity.Warning),
            InfoCount = items.Count(i => i.Severity == ComplianceItemSeverity.Info),
            ThisWeekCount = items.Count(i => i.DueDate.HasValue && i.DueDate.Value.Date >= today && i.DueDate.Value.Date <= weekEnd)
        };

        return viewModel;
    }

    public async Task<List<ComplianceCalendarItemDto>> GetItemsByMonthAsync(int clinicId, int year, int month)
    {
        var items = await GetAllItemsAsync(clinicId);
        return items
            .Where(i => i.DueDate.HasValue && i.DueDate.Value.Year == year && i.DueDate.Value.Month == month)
            .OrderBy(i => i.DaysRemaining)
            .ToList();
    }

    public async Task<List<ComplianceCalendarItemDto>> GetUpcomingItemsAsync(int clinicId, int days = 90)
    {
        var items = await GetAllItemsAsync(clinicId);
        var cutoff = DateTime.UtcNow.Date.AddDays(days);
        return items
            .Where(i => i.DueDate.HasValue && i.DueDate.Value.Date <= cutoff)
            .OrderBy(i => i.DaysRemaining)
            .ToList();
    }

    private async Task<List<ComplianceCalendarItemDto>> GetAllItemsAsync(int clinicId)
    {
        var items = new List<ComplianceCalendarItemDto>();
        var today = DateTime.UtcNow.Date;

        items.AddRange(await GetHRDocumentExpiryItemsAsync(clinicId, today));
        items.AddRange(await GetKPIDueItemsAsync(clinicId, today));
        items.AddRange(await GetChecklistDueItemsAsync(clinicId, today));
        items.AddRange(await GetClinicDocumentExpiryItemsAsync(clinicId, today));

        return items;
    }

    private async Task<List<ComplianceCalendarItemDto>> GetHRDocumentExpiryItemsAsync(int clinicId, DateTime today)
    {
        var items = new List<ComplianceCalendarItemDto>();
        var staff = await _unitOfWork.Repository<HrStaff>().FindAsync(
            s => s.ClinicId == clinicId && s.IsActive && !s.IsDeleted
        );
        var staffIds = staff.Select(s => s.Id).ToList();

        if (!staffIds.Any())
            return items;

        var documents = await _unitOfWork.Repository<HrDocument>().FindAsync(
            d => staffIds.Contains(d.HrStaffId) && d.ExpiryDate.HasValue && !d.IsDeleted
        );

        var staffDict = staff.ToDictionary(s => s.Id, s => s);

        foreach (var doc in documents)
        {
            var daysRemaining = (doc.ExpiryDate!.Value.Date - today).Days;
            var severity = daysRemaining <= 0 ? ComplianceItemSeverity.Critical
                : daysRemaining <= 14 ? ComplianceItemSeverity.Warning
                : ComplianceItemSeverity.Info;

            var staffMember = staffDict.GetValueOrDefault(doc.HrStaffId);

            items.Add(new ComplianceCalendarItemDto
            {
                Id = doc.Id,
                Title = doc.DocumentName,
                TitleAr = doc.DocumentNameAr,
                ItemType = ComplianceItemType.HRDocumentExpiry,
                Severity = severity,
                DueDate = doc.ExpiryDate,
                DaysRemaining = daysRemaining,
                SourceId = doc.Id,
                RelatedEntityName = staffMember?.FullNameEn,
                RelatedEntityNameAr = staffMember?.FullNameAr,
                DetailUrl = $"/ClinicAdmin/HRManagement/Details/{doc.HrStaffId}",
                Status = doc.IsVerified ? "Verified" : "Unverified"
            });
        }

        return items;
    }

    private async Task<List<ComplianceCalendarItemDto>> GetKPIDueItemsAsync(int clinicId, DateTime today)
    {
        var items = new List<ComplianceCalendarItemDto>();
        var kpis = await _unitOfWork.Repository<KPI>().FindAsync(
            k => k.ClinicId == clinicId && !k.IsDeleted
        );

        foreach (var kpi in kpis)
        {
            var (dueDate, daysRemaining) = CalculateKPIDueDate(kpi, today);
            if (!dueDate.HasValue)
                continue;

            var hasEntry = kpi.MonthlyEntries?.Any(e =>
                e.PeriodYear == dueDate.Value.Year && e.PeriodMonth == dueDate.Value.Month) ?? false;

            if (hasEntry)
                continue;

            var severity = daysRemaining <= 0 ? ComplianceItemSeverity.Critical
                : daysRemaining <= 7 ? ComplianceItemSeverity.Warning
                : ComplianceItemSeverity.Info;

            items.Add(new ComplianceCalendarItemDto
            {
                Id = kpi.Id,
                Title = kpi.Name,
                TitleAr = kpi.NameAr,
                ItemType = ComplianceItemType.KPIDue,
                Severity = severity,
                DueDate = dueDate,
                DaysRemaining = daysRemaining,
                SourceId = kpi.Id,
                DetailUrl = $"/ClinicAdmin/KPIManagement/EnterData/{kpi.Id}",
                Status = $"Target: {kpi.TargetValue}"
            });
        }

        return items;
    }

    private async Task<List<ComplianceCalendarItemDto>> GetChecklistDueItemsAsync(int clinicId, DateTime today)
    {
        var items = new List<ComplianceCalendarItemDto>();
        var templates = await _unitOfWork.Repository<ChecklistTemplate>().FindAsync(
            t => t.ClinicId == clinicId && t.IsActive && !t.IsDeleted
        );

        var templateIds = templates.Select(t => t.Id).ToList();
        var allRounds = await _unitOfWork.Repository<ChecklistRound>().FindAsync(
            r => templateIds.Contains(r.ChecklistTemplateId) && r.ClinicId == clinicId
        );
        var latestRounds = allRounds
            .GroupBy(r => r.ChecklistTemplateId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.ExecutedAt).First());

        foreach (var template in templates)
        {
            latestRounds.TryGetValue(template.Id, out var lastRound);

            var dueDate = CalculateChecklistDueDate(template, lastRound, today);
            if (!dueDate.HasValue)
                continue;

            var daysRemaining = (dueDate.Value.Date - today).Days;
            var severity = daysRemaining <= 0 ? ComplianceItemSeverity.Critical
                : daysRemaining <= 3 ? ComplianceItemSeverity.Warning
                : ComplianceItemSeverity.Info;

            items.Add(new ComplianceCalendarItemDto
            {
                Id = template.Id,
                Title = template.Name,
                TitleAr = template.NameAr,
                ItemType = ComplianceItemType.ChecklistDue,
                Severity = severity,
                DueDate = dueDate,
                DaysRemaining = daysRemaining,
                SourceId = template.Id,
                DetailUrl = $"/ClinicAdmin/ChecklistManagement/Execute/{template.Id}",
                Status = lastRound == null ? "Never Executed" : $"Last: {lastRound.ExecutedAt:yyyy-MM-dd}"
            });
        }

        return items;
    }

    private async Task<List<ComplianceCalendarItemDto>> GetClinicDocumentExpiryItemsAsync(int clinicId, DateTime today)
    {
        var items = new List<ComplianceCalendarItemDto>();
        var assignments = await _unitOfWork.Repository<ClinicTemplateAssignment>().FindAsync(
            a => a.ClinicId == clinicId && a.ExpiryDate.HasValue && !a.IsDeleted
        );

        var templateIds = assignments.Select(a => a.DocumentTemplateId).Distinct().ToList();
        var templates = templateIds.Any()
            ? await _unitOfWork.Repository<DocumentTemplate>().FindAsync(t => templateIds.Contains(t.Id))
            : new List<DocumentTemplate>();
        var templateDict = templates.ToDictionary(t => t.Id, t => t);

        foreach (var assignment in assignments)
        {
            var daysRemaining = (assignment.ExpiryDate!.Value.Date - today).Days;
            var severity = daysRemaining <= 0 ? ComplianceItemSeverity.Critical
                : daysRemaining <= 30 ? ComplianceItemSeverity.Warning
                : ComplianceItemSeverity.Info;

            var template = templateDict.GetValueOrDefault(assignment.DocumentTemplateId);

            items.Add(new ComplianceCalendarItemDto
            {
                Id = assignment.Id,
                Title = template?.TitleEn ?? $"Document #{assignment.Id}",
                TitleAr = template?.TitleAr,
                ItemType = ComplianceItemType.ClinicDocumentExpiry,
                Severity = severity,
                DueDate = assignment.ExpiryDate,
                DaysRemaining = daysRemaining,
                SourceId = assignment.Id,
                RelatedEntityName = template?.StandardCode,
                DetailUrl = $"/ClinicAdmin/ClinicDocuments/Details/{assignment.Id}",
                Status = assignment.AssignmentStatus.ToString()
            });
        }

        return items;
    }

    private static (DateTime? DueDate, int DaysRemaining) CalculateKPIDueDate(KPI kpi, DateTime today)
    {
        var now = today;
        DateTime dueDate;

        switch (kpi.Frequency)
        {
            case KPIFrequency.Daily:
                dueDate = now;
                break;
            case KPIFrequency.Weekly:
                var daysUntilSunday = ((int)DayOfWeek.Sunday - (int)now.DayOfWeek + 7) % 7;
                dueDate = daysUntilSunday == 0 ? now : now.AddDays(daysUntilSunday);
                break;
            case KPIFrequency.Monthly:
                dueDate = new DateTime(now.Year, now.Month, 1).AddMonths(1).AddDays(-1);
                break;
            case KPIFrequency.Quarterly:
                var quarterEnd = ((now.Month - 1) / 3 + 1) * 3;
                dueDate = new DateTime(now.Year, quarterEnd, 1).AddMonths(1).AddDays(-1);
                break;
            case KPIFrequency.Annually:
                dueDate = new DateTime(now.Year, 12, 31);
                break;
            default:
                return (null, 0);
        }

        var daysRemaining = (dueDate - today).Days;
        return (dueDate, daysRemaining);
    }

    private static DateTime? CalculateChecklistDueDate(ChecklistTemplate template, ChecklistRound? lastRound, DateTime today)
    {
        var frequencyDays = template.Frequency switch
        {
            ChecklistSchedule.Daily => 1,
            ChecklistSchedule.Weekly => 7,
            ChecklistSchedule.Monthly => 30,
            _ => 30
        };

        if (lastRound == null)
            return today;

        var nextDue = lastRound.ExecutedAt.Date.AddDays(frequencyDays);
        return nextDue <= today ? today : nextDue;
    }
}
