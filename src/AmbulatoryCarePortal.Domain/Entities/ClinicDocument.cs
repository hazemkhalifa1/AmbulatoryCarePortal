using AmbulatoryCarePortal.Domain.Enums;

namespace AmbulatoryCarePortal.Domain.Entities;

public class ClinicDocument : BaseEntity
{
    public int ClinicId { get; set; }
    public int DocumentTemplateId { get; set; }
    public ClinicDocumentStatus DocumentStatus { get; set; } = ClinicDocumentStatus.NeedsReview;
    public DateTime? ExpiryDate { get; set; }
    public string? OfficialPdfPath { get; set; }
    public string? Notes { get; set; }

    // Navigation properties
    public Clinic Clinic { get; set; } = null!;
    public DocumentTemplate DocumentTemplate { get; set; } = null!;
    public ICollection<ClinicDocumentAttachment> Attachments { get; set; } = new List<ClinicDocumentAttachment>();
}
