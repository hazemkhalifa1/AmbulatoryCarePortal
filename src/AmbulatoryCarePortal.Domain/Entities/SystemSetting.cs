using AmbulatoryCarePortal.Domain.Enums;

namespace AmbulatoryCarePortal.Domain.Entities;

public class SystemSetting : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
    public SettingCategory Category { get; set; }
    public SettingValueType ValueType { get; set; }
    public string? Description { get; set; }
    public bool IsEncrypted { get; set; }
}
