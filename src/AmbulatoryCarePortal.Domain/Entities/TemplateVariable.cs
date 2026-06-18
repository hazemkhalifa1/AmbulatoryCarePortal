namespace AmbulatoryCarePortal.Domain.Entities;

public class TemplateVariable : BaseEntity
{
    public int DocumentTemplateId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsImage { get; set; }
    public bool IsRequired { get; set; }
    public string? DefaultValue { get; set; }

    public DocumentTemplate DocumentTemplate { get; set; } = null!;
    public ICollection<ClinicTemplateValue> ClinicValues { get; set; } = new List<ClinicTemplateValue>();
}
