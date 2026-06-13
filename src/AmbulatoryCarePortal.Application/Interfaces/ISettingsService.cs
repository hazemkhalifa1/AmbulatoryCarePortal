using AmbulatoryCarePortal.Application.DTOs;
using AmbulatoryCarePortal.Domain.Enums;

namespace AmbulatoryCarePortal.Application.Interfaces;

public interface ISettingsService
{
    Task<string?> GetValueAsync(string key);
    Task<T?> GetValueAsync<T>(string key, T? defaultValue = default);
    Task SetValueAsync(string key, string? value);
    Task<List<SystemSettingDto>> GetByCategoryAsync(SettingCategory category);
    Task<Dictionary<SettingCategory, List<SystemSettingDto>>> GetAllGroupedAsync();
}
