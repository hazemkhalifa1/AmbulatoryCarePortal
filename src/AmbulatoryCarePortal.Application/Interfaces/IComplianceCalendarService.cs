using AmbulatoryCarePortal.Application.DTOs;

namespace AmbulatoryCarePortal.Application.Interfaces;

public interface IComplianceCalendarService
{
    Task<ComplianceCalendarViewModel> GetCalendarAsync(int clinicId);
    Task<List<ComplianceCalendarItemDto>> GetItemsByMonthAsync(int clinicId, int year, int month);
    Task<List<ComplianceCalendarItemDto>> GetUpcomingItemsAsync(int clinicId, int days = 90);
}
