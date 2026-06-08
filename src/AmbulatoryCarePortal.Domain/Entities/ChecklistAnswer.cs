using AmbulatoryCarePortal.Domain.Enums;

namespace AmbulatoryCarePortal.Domain.Entities;

public class ChecklistAnswer : BaseEntity
{
    public int ChecklistRoundId { get; set; }
    public int ChecklistItemId { get; set; }
    public Enums.ChecklistAnswer AnswerValue { get; set; } = Enums.ChecklistAnswer.NA;
    public string? Notes { get; set; }
    public string? EvidenceFilePath { get; set; }
    public string? OwnerId { get; set; }

    // Navigation properties
    public ChecklistRound ChecklistRound { get; set; } = null!;
    public ChecklistItem ChecklistItem { get; set; } = null!;
    public AppUser? Owner { get; set; }
}
