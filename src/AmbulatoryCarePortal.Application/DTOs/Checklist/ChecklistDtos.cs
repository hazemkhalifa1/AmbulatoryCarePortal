using AmbulatoryCarePortal.Domain.Enums;

namespace AmbulatoryCarePortal.Application.DTOs;

public class CreateChecklistTemplateDto
{
    public string Name { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public int ClinicId { get; set; }
    public int? DepartmentId { get; set; }
    public ChecklistSchedule Frequency { get; set; }
    public List<CreateChecklistItemDto> Items { get; set; } = new();
}

public class CreateChecklistItemDto
{
    public string Question { get; set; } = string.Empty;
    public string? QuestionAr { get; set; }
    public int SortOrder { get; set; }
}

public class ChecklistTemplateDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public ChecklistSchedule Frequency { get; set; }
    public bool IsActive { get; set; }
    public int ItemCount { get; set; }
}

public class CreateChecklistRoundDto
{
    public int ChecklistTemplateId { get; set; }
    public int ClinicId { get; set; }
    public int? DepartmentId { get; set; }
    public List<CreateChecklistAnswerDto> Answers { get; set; } = new();
}

public class CreateChecklistAnswerDto
{
    public int ChecklistItemId { get; set; }
    public ChecklistAnswer Answer { get; set; }
    public string? Notes { get; set; }
}

public class ChecklistRoundDto
{
    public int Id { get; set; }
    public string ChecklistName { get; set; } = string.Empty;
    public DateTime ExecutedAt { get; set; }
    public string? ExecutedBy { get; set; }
    public int CompletionPercentage { get; set; }
}
