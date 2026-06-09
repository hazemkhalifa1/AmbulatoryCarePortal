using AmbulatoryCarePortal.Application.Interfaces;
using AmbulatoryCarePortal.Domain.Entities;
using AmbulatoryCarePortal.Presentation.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AmbulatoryCarePortal.Presentation.Areas.ClinicAdmin.Controllers;

[Area("ClinicAdmin")]
[Authorize(Roles = "ClinicAdmin,ClinicViewer")]
public class NotificationsController : Controller
{
    private readonly INotificationService _notificationService;
    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(
        INotificationService notificationService,
        ILogger<NotificationsController> logger,
        UserManager<AppUser> userManager)
    {
        _notificationService = notificationService;
        _logger = logger;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        var userId = user?.Id;
        var clinicId = user?.ClinicId ?? 0;
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
