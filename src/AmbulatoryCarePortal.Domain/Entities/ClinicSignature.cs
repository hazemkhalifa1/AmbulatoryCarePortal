using AmbulatoryCarePortal.Domain.Enums;

namespace AmbulatoryCarePortal.Domain.Entities;

public class ClinicSignature : BaseEntity
{
    public int ClinicId { get; set; }
    public string SignerCode { get; set; } = string.Empty;
    public string SignerName { get; set; } = string.Empty;
    public string SignerTitle { get; set; } = string.Empty;
    public string? SignatureImagePath { get; set; }
    public SignatureType SignatureType { get; set; }
    public bool IsActive { get; set; } = true;

    public Clinic Clinic { get; set; } = null!;
}
