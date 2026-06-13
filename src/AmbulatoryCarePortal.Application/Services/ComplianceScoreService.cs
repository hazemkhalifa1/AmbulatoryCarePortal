using AmbulatoryCarePortal.Application.DTOs.Analytics;
using AmbulatoryCarePortal.Application.Interfaces;
using AmbulatoryCarePortal.Domain.Entities;
using AmbulatoryCarePortal.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace AmbulatoryCarePortal.Application.Services;

public class ComplianceScoreService : IComplianceScoreService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISettingsService _settings;
    private readonly ILogger<ComplianceScoreService> _logger;

    public ComplianceScoreService(
        IUnitOfWork unitOfWork,
        ISettingsService settings,
        ILogger<ComplianceScoreService> logger)
    {
        _unitOfWork = unitOfWork;
        _settings = settings;
        _logger = logger;
    }

    public async Task<ComplianceScoreDto> CalculateScoreAsync(int clinicId)
    {
        var clinic = await _unitOfWork.Repository<Clinic>().GetByIdAsync(clinicId);
        if (clinic == null)
            throw new InvalidOperationException($"Clinic {clinicId} not found");

        var (policyScore, kpiScore, checklistScore, hrScore, documentScore) = await CalculateComponentsAsync(clinicId);

        var policyW = await _settings.GetValueAsync("Compliance.Weight.Policy", 25m);
        var kpiW = await _settings.GetValueAsync("Compliance.Weight.KPI", 20m);
        var checklistW = await _settings.GetValueAsync("Compliance.Weight.Checklist", 25m);
        var hrW = await _settings.GetValueAsync("Compliance.Weight.HR", 20m);
        var docW = await _settings.GetValueAsync("Compliance.Weight.Document", 10m);
        var totalW = policyW + kpiW + checklistW + hrW + docW;

        var overall = totalW > 0
            ? (policyScore * policyW + kpiScore * kpiW + checklistScore * checklistW + hrScore * hrW + documentScore * docW) / totalW
            : 0;

        overall = Math.Round(Math.Clamp(overall, 0, 100), 1);

        clinic.ComplianceScore = overall;
        _unitOfWork.Repository<Clinic>().Update(clinic);

        var snapshot = new ComplianceScoreSnapshot
        {
            ClinicId = clinicId,
            OverallScore = overall,
            PolicyScore = Math.Round(policyScore, 1),
            KpiScore = Math.Round(kpiScore, 1),
            ChecklistScore = Math.Round(checklistScore, 1),
            HrScore = Math.Round(hrScore, 1),
            DocumentScore = Math.Round(documentScore, 1),
            PolicyWeight = policyW,
            KpiWeight = kpiW,
            ChecklistWeight = checklistW,
            HrWeight = hrW,
            DocumentWeight = docW,
            CalculatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Repository<ComplianceScoreSnapshot>().AddAsync(snapshot);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Compliance score calculated for clinic {ClinicId}: {Score}%", clinicId, overall);

        return new ComplianceScoreDto
        {
            ClinicId = clinicId,
            ClinicName = clinic.Name,
            OverallScore = overall,
            CalculatedAt = snapshot.CalculatedAt,
            Components =
            [
                new ScoreComponentDto { Name = "Policies", NameAr = "السياسات", Score = Math.Round(policyScore, 1), Weight = policyW, Color = "#2196F3", Icon = "bi-file-text" },
                new ScoreComponentDto { Name = "KPIs", NameAr = "المؤشرات", Score = Math.Round(kpiScore, 1), Weight = kpiW, Color = "#4CAF50", Icon = "bi-bar-chart" },
                new ScoreComponentDto { Name = "Checklists", NameAr = "قوائم التحقق", Score = Math.Round(checklistScore, 1), Weight = checklistW, Color = "#9C27B0", Icon = "bi-check2-square" },
                new ScoreComponentDto { Name = "HR Credentials", NameAr = "مؤهلات الموظفين", Score = Math.Round(hrScore, 1), Weight = hrW, Color = "#FF9800", Icon = "bi-people" },
                new ScoreComponentDto { Name = "Documents", NameAr = "المستندات", Score = Math.Round(documentScore, 1), Weight = docW, Color = "#F44336", Icon = "bi-folder" },
            ]
        };
    }

    public async Task<ComplianceScoreDto> GetLatestScoreAsync(int clinicId)
    {
        var clinic = await _unitOfWork.Repository<Clinic>().GetByIdAsync(clinicId);
        if (clinic == null)
            throw new InvalidOperationException($"Clinic {clinicId} not found");

        var snapshots = await _unitOfWork.Repository<ComplianceScoreSnapshot>().FindAsync(
            s => s.ClinicId == clinicId
        );
        var latest = snapshots.OrderByDescending(s => s.CalculatedAt).FirstOrDefault();

        if (latest == null)
            return await CalculateScoreAsync(clinicId);

        return new ComplianceScoreDto
        {
            ClinicId = clinicId,
            ClinicName = clinic.Name,
            OverallScore = latest.OverallScore,
            CalculatedAt = latest.CalculatedAt,
            Components =
            [
                new ScoreComponentDto { Name = "Policies", NameAr = "السياسات", Score = latest.PolicyScore, Weight = latest.PolicyWeight, Color = "#2196F3", Icon = "bi-file-text" },
                new ScoreComponentDto { Name = "KPIs", NameAr = "المؤشرات", Score = latest.KpiScore, Weight = latest.KpiWeight, Color = "#4CAF50", Icon = "bi-bar-chart" },
                new ScoreComponentDto { Name = "Checklists", NameAr = "قوائم التحقق", Score = latest.ChecklistScore, Weight = latest.ChecklistWeight, Color = "#9C27B0", Icon = "bi-check2-square" },
                new ScoreComponentDto { Name = "HR Credentials", NameAr = "مؤهلات الموظفين", Score = latest.HrScore, Weight = latest.HrWeight, Color = "#FF9800", Icon = "bi-people" },
                new ScoreComponentDto { Name = "Documents", NameAr = "المستندات", Score = latest.DocumentScore, Weight = latest.DocumentWeight, Color = "#F44336", Icon = "bi-folder" },
            ]
        };
    }

    public async Task<List<ComplianceScoreSnapshotDto>> GetScoreHistoryAsync(int clinicId, int count = 10)
    {
        var snapshots = await _unitOfWork.Repository<ComplianceScoreSnapshot>().FindAsync(
            s => s.ClinicId == clinicId
        );

        return snapshots
            .OrderByDescending(s => s.CalculatedAt)
            .Take(count)
            .Select(s => new ComplianceScoreSnapshotDto
            {
                Id = s.Id,
                ClinicId = s.ClinicId,
                OverallScore = s.OverallScore,
                PolicyScore = s.PolicyScore,
                KpiScore = s.KpiScore,
                ChecklistScore = s.ChecklistScore,
                HrScore = s.HrScore,
                DocumentScore = s.DocumentScore,
                CalculatedAt = s.CalculatedAt
            })
            .ToList();
    }

    public async Task<List<ScoreTrendDto>> GetScoreTrendAsync(int clinicId, int days = 90)
    {
        var since = DateTime.UtcNow.AddDays(-days);
        var snapshots = await _unitOfWork.Repository<ComplianceScoreSnapshot>().FindAsync(
            s => s.ClinicId == clinicId && s.CalculatedAt >= since
        );

        return snapshots
            .OrderBy(s => s.CalculatedAt)
            .Select(s => new ScoreTrendDto { Date = s.CalculatedAt, Score = s.OverallScore })
            .ToList();
    }

    public async Task<ComplianceDashboardDto> GetDashboardAsync(int clinicId)
    {
        var policyRepo = _unitOfWork.Repository<PolicyDocument>();
        var hrDocRepo = _unitOfWork.Repository<HrDocument>();
        var roundRepo = _unitOfWork.Repository<ChecklistRound>();

        var missingPolicies = await policyRepo.CountAsync(
            p => p.ClinicId == clinicId && p.DocumentStatus == DocumentStatus.MissingAttachment);
        var expiredDocs = await hrDocRepo.CountAsync(
            d => d.HrStaff.ClinicId == clinicId && d.ExpiryDate.HasValue && d.ExpiryDate < DateTime.UtcNow);
        var overdueChecklists = await roundRepo.CountAsync(
            r => r.ClinicId == clinicId && r.ExecutedAt < DateTime.UtcNow.AddMonths(-1));

        var upcomingExpiry = DateTime.UtcNow.AddDays(30);
        var expiringCerts = await hrDocRepo.CountAsync(
            d => d.HrStaff.ClinicId == clinicId && d.ExpiryDate.HasValue && d.ExpiryDate <= upcomingExpiry && d.ExpiryDate >= DateTime.UtcNow);

        return new ComplianceDashboardDto
        {
            CurrentScore = await GetLatestScoreAsync(clinicId),
            Trend = await GetScoreTrendAsync(clinicId),
            MissingPolicies = missingPolicies,
            ExpiredDocuments = expiredDocs,
            OverdueChecklists = overdueChecklists,
            ExpiringCredentials = expiringCerts
        };
    }

    public async Task<List<ComplianceScoreDto>> GetAllClinicsScoresAsync()
    {
        var clinics = await _unitOfWork.Repository<Clinic>().FindAsync(c => c.IsActive);
        var results = new List<ComplianceScoreDto>();

        foreach (var clinic in clinics)
            results.Add(await GetLatestScoreAsync(clinic.Id));

        return results;
    }

    private async Task<(decimal Policy, decimal Kpi, decimal Checklist, decimal Hr, decimal Document)> CalculateComponentsAsync(int clinicId)
    {
        var policyScore = await CalculatePolicyScoreAsync(clinicId);
        var kpiScore = await CalculateKpiScoreAsync(clinicId);
        var checklistScore = await CalculateChecklistScoreAsync(clinicId);
        var hrScore = await CalculateHrScoreAsync(clinicId);
        var documentScore = await CalculateDocumentScoreAsync(clinicId);

        return (policyScore, kpiScore, checklistScore, hrScore, documentScore);
    }

    private async Task<decimal> CalculatePolicyScoreAsync(int clinicId)
    {
        var total = await _unitOfWork.Repository<PolicyDocument>().CountAsync(p => p.ClinicId == clinicId);
        if (total == 0) return 0;

        var compliant = await _unitOfWork.Repository<PolicyDocument>().CountAsync(p =>
            p.ClinicId == clinicId &&
            (p.DocumentStatus == DocumentStatus.Approved || p.DocumentStatus == DocumentStatus.Complete) &&
            (!p.ExpiryDate.HasValue || p.ExpiryDate >= DateTime.UtcNow));

        return (compliant * 100m) / total;
    }

    private async Task<decimal> CalculateKpiScoreAsync(int clinicId)
    {
        var kpis = await _unitOfWork.Repository<KPI>().FindAsync(k => k.ClinicId == clinicId);
        var kpiList = kpis.ToList();
        if (kpiList.Count == 0) return 0;
        var kpiIds = kpiList.Select(k => k.Id).ToList();
        var entries = await _unitOfWork.Repository<KPIEntry>().FindAsync(
            e => kpiIds.Contains(e.KPIId));

        var entryGroups = entries.GroupBy(e => e.KPIId).ToList();

        var totalScore = 0m;
        var scoredCount = 0;

        foreach (var kpi in kpiList)
        {
            var group = entryGroups.FirstOrDefault(g => g.Key == kpi.Id);
            if (group == null) continue;

            var latest = group.OrderByDescending(e => e.PeriodYear).ThenByDescending(e => e.PeriodMonth).First();
            if (kpi.TargetValue > 0)
            {
                totalScore += Math.Min(latest.ActualValue / kpi.TargetValue * 100m, 100);
                scoredCount++;
            }
        }

        return scoredCount > 0 ? totalScore / scoredCount : 0;
    }

    private async Task<decimal> CalculateChecklistScoreAsync(int clinicId)
    {
        var templates = await _unitOfWork.Repository<ChecklistTemplate>().FindAsync(
            t => t.ClinicId == clinicId && t.IsActive);
        var templateList = templates.ToList();
        if (templateList.Count == 0) return 100;

        var templateIds = templateList.Select(t => t.Id).ToList();
        var rounds = await _unitOfWork.Repository<ChecklistRound>().FindAsync(
            r => templateIds.Contains(r.ChecklistTemplateId));

        var groupByTemplate = rounds.GroupBy(r => r.ChecklistTemplateId).ToList();

        var compliant = 0;
        foreach (var template in templateList)
        {
            var group = groupByTemplate.FirstOrDefault(g => g.Key == template.Id);
            if (group == null)
            {
                compliant += 0;
                continue;
            }

            var lastRound = group.OrderByDescending(r => r.ExecutedAt).FirstOrDefault();
            if (lastRound != null)
            {
                var daysSinceLastRun = (DateTime.UtcNow - lastRound.ExecutedAt).TotalDays;
                if (daysSinceLastRun <= (int)template.Frequency * 1.2)
                    compliant++;
            }
        }

        return templateList.Count > 0 ? (compliant * 100m) / templateList.Count : 100;
    }

    private async Task<decimal> CalculateHrScoreAsync(int clinicId)
    {
        var staff = (await _unitOfWork.Repository<HrStaff>().FindAsync(s => s.ClinicId == clinicId)).ToList();
        if (staff.Count == 0) return 100;

        var staffIds = staff.Select(s => s.Id).ToList();
        var documents = await _unitOfWork.Repository<HrDocument>().FindAsync(
            d => staffIds.Contains(d.HrStaffId));

        var staffWithValidDocs = 0;
        foreach (var s in staff)
        {
            var staffDocs = documents.Where(d => d.HrStaffId == s.Id).ToList();
            if (staffDocs.Count == 0) continue;

            var allValid = staffDocs.All(d =>
                d.IsVerified &&
                (!d.ExpiryDate.HasValue || d.ExpiryDate >= DateTime.UtcNow));

            if (allValid) staffWithValidDocs++;
        }

        return staff.Count > 0 ? (staffWithValidDocs * 100m) / staff.Count : 100;
    }

    private async Task<decimal> CalculateDocumentScoreAsync(int clinicId)
    {
        var templateCount = await _unitOfWork.Repository<DocumentTemplate>().CountAsync(t => t.IsActive);
        if (templateCount == 0) return 100;

        var clinicDocs = (await _unitOfWork.Repository<ClinicDocument>().FindAsync(
            d => d.ClinicId == clinicId)).ToList();

        var validDocs = clinicDocs.Count(d =>
            d.DocumentStatus == ClinicDocumentStatus.Complete &&
            (!d.ExpiryDate.HasValue || d.ExpiryDate >= DateTime.UtcNow));

        return templateCount > 0 ? (validDocs * 100m) / templateCount : 100;
    }
}
