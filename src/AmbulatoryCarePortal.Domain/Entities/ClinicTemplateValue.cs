namespace AmbulatoryCarePortal.Domain.Entities;

public class ClinicTemplateValue : BaseEntity
{
    public int ClinicTemplateAssignmentId { get; set; }
    public int TemplateVariableId { get; set; }
    public string? Value { get; set; }
    public string? ImagePath { get; set; }

    public ClinicTemplateAssignment ClinicTemplateAssignment { get; set; } = null!;
    public TemplateVariable TemplateVariable { get; set; } = null!;
}
