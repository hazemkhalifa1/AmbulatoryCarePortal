using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AmbulatoryCarePortal.Application.Interfaces;
using AmbulatoryCarePortal.Presentation.Extensions;

namespace AmbulatoryCarePortal.Presentation.Areas.ClinicAdmin.Controllers;

[Area("ClinicAdmin")]
[Authorize(Roles = "HospitalAdmin,ClinicAdmin,DepartmentUser,Auditor,Viewer")]
public class NotificationsController : Controller
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(
        INotificationService notificationService,
        ILogger<NotificationsController> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = User.GetUserId();
        var clinicId = User.GetClinicId() ?? 0;
        var notifications = await _notificationService.GetUserNotificationsAsync(userId);
        ViewBag.UnreadCount = await _notificationService.GetUnreadCountAsync(clinicId, userId);
        ViewBag.PageTitle = "Notifications";
        return View(notifications);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(int id)
    {
        try
        {
            await _notificationService.MarkAsReadAsync(id);
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification as read");
            return Json(new { success = false });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead()
    {
        try
        {
            var userId = User.GetUserId();
            await _notificationService.MarkAllAsReadAsync(userId);
            TempData["SuccessMessage"] = "All notifications marked as read";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all notifications as read");
            TempData["ErrorMessage"] = "An error occurred";
        }

        return RedirectToAction(nameof(Index));
    }
}
