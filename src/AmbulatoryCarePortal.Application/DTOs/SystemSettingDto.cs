using AmbulatoryCarePortal.Domain.Enums;

namespace AmbulatoryCarePortal.Application.DTOs;

public class SystemSettingDto
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
    public SettingCategory Category { get; set; }
    public SettingValueType ValueType { get; set; }
    public string? Description { get; set; }
    public bool IsEncrypted { get; set; }
}
