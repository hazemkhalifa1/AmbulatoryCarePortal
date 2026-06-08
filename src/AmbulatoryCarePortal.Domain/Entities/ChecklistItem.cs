namespace AmbulatoryCarePortal.Domain.Entities;

public class ChecklistItem : BaseEntity
{
    public int ChecklistTemplateId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string? QuestionTextAr { get; set; }
    public int ItemOrder { get; set; }
    public int Weight { get; set; } = 1;

    // Navigation properties
    public ChecklistTemplate ChecklistTemplate { get; set; } = null!;
    public ICollection<ChecklistAnswer> Answers { get; set; } = new List<ChecklistAnswer>();
}
