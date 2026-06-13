using AmbulatoryCarePortal.Application.DTOs;
using AmbulatoryCarePortal.Application.Interfaces;
using AmbulatoryCarePortal.Domain.Entities;
using AmbulatoryCarePortal.Domain.Enums;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace AmbulatoryCarePortal.Application.Services;

public class SettingsService : ISettingsService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEncryptionService _encryption;
    private readonly IMapper _mapper;
    private readonly ILogger<SettingsService> _logger;
    private readonly ICacheService _cache;

    private static readonly TimeSpan SettingsTtl = TimeSpan.FromMinutes(15);

    public SettingsService(
        IUnitOfWork unitOfWork,
        IEncryptionService encryption,
        IMapper mapper,
        ILogger<SettingsService> logger,
        ICacheService cache)
    {
        _unitOfWork = unitOfWork;
        _encryption = encryption;
        _mapper = mapper;
        _logger = logger;
        _cache = cache;
    }

    public async Task<string?> GetValueAsync(string key)
    {
        var cacheKey = $"settings:{key}";
        var cached = await _cache.GetAsync<string>(cacheKey);
        if (cached != null)
            return cached;

        var setting = await _unitOfWork.Repository<SystemSetting>()
            .FirstOrDefaultAsync(s => s.Key == key);

        if (setting == null)
            return null;

        var value = setting.IsEncrypted && !string.IsNullOrEmpty(setting.Value)
            ? _encryption.Decrypt(setting.Value)
            : setting.Value;

        if (value != null)
            await _cache.SetAsync(cacheKey, value, SettingsTtl);

        return value;
    }

    public async Task<T?> GetValueAsync<T>(string key, T? defaultValue = default)
    {
        var value = await GetValueAsync(key);
        if (value == null)
            return defaultValue;

        try
        {
            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch
        {
            return defaultValue;
        }
    }

    public async Task SetValueAsync(string key, string? value)
    {
        var setting = await _unitOfWork.Repository<SystemSetting>()
            .FirstOrDefaultAsync(s => s.Key == key);

        if (setting == null)
        {
            setting = new SystemSetting
            {
                Key = key,
                Value = value,
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.Repository<SystemSetting>().AddAsync(setting);
        }
        else
        {
            setting.Value = value;
            setting.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Repository<SystemSetting>().Update(setting);
        }

        await _unitOfWork.SaveChangesAsync();

        var cacheKey = $"settings:{key}";
        if (value != null)
            await _cache.SetAsync(cacheKey, value, SettingsTtl);
        else
            await _cache.RemoveAsync(cacheKey);
    }

    public async Task<List<SystemSettingDto>> GetByCategoryAsync(SettingCategory category)
    {
        var cacheKey = $"settings:category:{category}";
        var cached = await _cache.GetAsync<List<SystemSettingDto>>(cacheKey);
        if (cached != null)
            return cached;

        var settings = await _unitOfWork.Repository<SystemSetting>()
            .FindAsync(s => s.Category == category);

        var dtos = _mapper.Map<List<SystemSettingDto>>(settings.ToList());

        foreach (var dto in dtos)
        {
            if (dto.IsEncrypted && !string.IsNullOrEmpty(dto.Value))
            {
                dto.Value = _encryption.Decrypt(dto.Value);
            }
        }

        await _cache.SetAsync(cacheKey, dtos, SettingsTtl);
        return dtos;
    }

    public async Task<Dictionary<SettingCategory, List<SystemSettingDto>>> GetAllGroupedAsync()
    {
        var cacheKey = "settings:all";
        var cached = await _cache.GetAsync<Dictionary<SettingCategory, List<SystemSettingDto>>>(cacheKey);
        if (cached != null)
            return cached;

        var settings = await _unitOfWork.Repository<SystemSetting>().GetAllAsync();
        var dtos = _mapper.Map<List<SystemSettingDto>>(settings.ToList());

        foreach (var dto in dtos)
        {
            if (dto.IsEncrypted && !string.IsNullOrEmpty(dto.Value))
            {
                dto.Value = _encryption.Decrypt(dto.Value);
            }
        }

        var result = dtos.GroupBy(s => s.Category)
            .ToDictionary(g => g.Key, g => g.ToList());

        await _cache.SetAsync(cacheKey, result, SettingsTtl);
        return result;
    }
}
