using AmbulatoryCarePortal.Domain.Enums;

namespace AmbulatoryCarePortal.Application.DTOs;

public class ComplianceCalendarItemDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? TitleAr { get; set; }
    public ComplianceItemType ItemType { get; set; }
    public ComplianceItemSeverity Severity { get; set; }
    public DateTime? DueDate { get; set; }
    public int DaysRemaining { get; set; }
    public int SourceId { get; set; }
    public string? RelatedEntityName { get; set; }
    public string? RelatedEntityNameAr { get; set; }
    public string? DetailUrl { get; set; }
    public string? Status { get; set; }
}

public class ComplianceCalendarViewModel
{
    public List<ComplianceCalendarItemDto> Items { get; set; } = new();
    public int CriticalCount { get; set; }
    public int WarningCount { get; set; }
    public int InfoCount { get; set; }
    public int ThisWeekCount { get; set; }
}
