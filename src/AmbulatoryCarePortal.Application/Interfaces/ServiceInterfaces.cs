using AmbulatoryCarePortal.Application.Common;
using AmbulatoryCarePortal.Application.DTOs;
using AmbulatoryCarePortal.Application.DTOs.Clinic;

namespace AmbulatoryCarePortal.Application.Interfaces;

public interface IClinicService
{
    Task<PagedResult<ClinicDto>> GetAllClinicsAsync(int pageNumber, int pageSize);
    Task<ClinicDetailDto?> GetClinicDetailsAsync(int clinicId);
    Task<int> CreateClinicAsync(CreateClinicDto dto);
    Task<bool> UpdateClinicAsync(UpdateClinicDto dto);
    Task<bool> DeleteClinicAsync(int clinicId);
}

public interface IKPIService
{
    Task<List<KPIDto>> GetClinicKPIsAsync(int clinicId);
    Task<List<KPIDto>> GetDepartmentKPIsAsync(int departmentId);
    Task<int> CreateKPIAsync(CreateKPIDto dto);
    Task<bool> UpdateKPIAsync(int id, CreateKPIDto dto);
    Task<bool> DeleteKPIAsync(int kpiId);
    Task<bool> AddKPIEntryAsync(int kpiId, int month, int year, decimal actualValue);
}

public interface IChecklistService
{
    Task<List<ChecklistTemplateDto>> GetClinicChecklistsAsync(int clinicId);
    Task<int> CreateChecklistAsync(CreateChecklistTemplateDto dto);
    Task<bool> UpdateChecklistAsync(int id, CreateChecklistTemplateDto dto);
    Task<bool> DeleteChecklistAsync(int checklistId);
    Task<int> ExecuteChecklistAsync(CreateChecklistRoundDto dto, string userId);
    Task<List<ChecklistRoundDto>> GetChecklistHistoryAsync(int templateId, int pageSize = 10);
}

public interface IHrService
{
    Task<PagedResult<HrStaffDto>> GetClinicStaffAsync(int clinicId, int pageNumber, int pageSize);
    Task<HrStaffDetailDto?> GetStaffDetailsAsync(int staffId);
    Task<int> CreateStaffAsync(CreateHrStaffDto dto);
    Task<bool> UpdateStaffAsync(int id, CreateHrStaffDto dto);
    Task<bool> DeleteStaffAsync(int staffId);
    Task<bool> AddDocumentAsync(CreateHrDocumentDto dto);
    Task<List<HrDocumentDto>> GetExpiringDocumentsAsync(int clinicId, int daysThreshold = 30);
}

public interface IAuditService
{
    Task LogActionAsync(int clinicId, string actionType, string? description, string? targetObjectType, int? targetObjectId, string? userId, string? ipAddress);
    Task<PagedResult<AuditTrailDto>> GetAuditTrailAsync(int clinicId, int pageNumber = 1, int pageSize = 20, string? searchTerm = null, string? actionTypeFilter = null, DateTime? dateFrom = null, DateTime? dateTo = null);
    Task<AuditTrailDto?> GetAuditTrailByIdAsync(int id);
    Task<int> GetAuditLogCountAsync(int clinicId);
    Task<int> GetDistinctUserCountAsync(int clinicId);
}

public interface INotificationService
{
    Task SendNotificationAsync(int clinicId, string title, string message, string? messageAr, string notificationType, int? targetObjectId, string targetObjectType);
    Task<List<NotificationDto>> GetUserNotificationsAsync(string userId);
    Task MarkAsReadAsync(int notificationId);
    Task MarkAllAsReadAsync(string userId);
    Task<int> GetUnreadCountAsync(int clinicId, string userId);
}
