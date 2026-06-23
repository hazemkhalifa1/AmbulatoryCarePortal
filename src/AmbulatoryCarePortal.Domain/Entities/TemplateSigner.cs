namespace AmbulatoryCarePortal.Domain.Entities;

public class TemplateSigner : BaseEntity
{
    public int DocumentTemplateId { get; set; }
    public string SignerCode { get; set; } = string.Empty;
    public string SignerDisplayName { get; set; } = string.Empty;
    public string SignerTitle { get; set; } = string.Empty;
    public bool IsRequired { get; set; } = true;

    public DocumentTemplate DocumentTemplate { get; set; } = null!;
}
