using AmbulatoryCarePortal.Domain.Enums;

namespace AmbulatoryCarePortal.Domain.Entities;

public class ClinicTemplateAssignment : BaseEntity
{
    public int ClinicId { get; set; }
    public int DocumentTemplateId { get; set; }
    public ClinicDocumentStatus AssignmentStatus { get; set; } = ClinicDocumentStatus.NeedsReview;
    public DateTime? ExpiryDate { get; set; }
    public string? Notes { get; set; }

    public Clinic Clinic { get; set; } = null!;
    public DocumentTemplate DocumentTemplate { get; set; } = null!;
    public ICollection<ClinicTemplateValue> VariableValues { get; set; } = new List<ClinicTemplateValue>();
    public ICollection<GeneratedDocument> GeneratedDocuments { get; set; } = new List<GeneratedDocument>();
}
