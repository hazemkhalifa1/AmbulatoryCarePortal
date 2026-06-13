using AmbulatoryCarePortal.Application.Interfaces;
using AmbulatoryCarePortal.Domain.Enums;
using AmbulatoryCarePortal.Presentation.Helpers;
using AmbulatoryCarePortal.Presentation.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AmbulatoryCarePortal.Presentation.Areas.SuperAdmin.Controllers;

[Area("SuperAdmin")]
[Authorize(Policy = "Permission.system.configure")]
public class SettingsController : Controller
{
    private readonly ISettingsService _settingsService;
    private readonly IEmailService _emailService;
    private readonly ITranslationService _localizer;
    private readonly ILogger<SettingsController> _logger;

    public SettingsController(
        ISettingsService settingsService,
        IEmailService emailService,
        ITranslationService localizer,
        ILogger<SettingsController> logger)
    {
        _settingsService = settingsService;
        _emailService = emailService;
        _localizer = localizer;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string tab = "mail")
    {
        ViewBag.PageTitle = _localizer.T("Page.SystemSettings");
        ViewBag.ActiveTab = tab;
        ViewBag.Settings = await _settingsService.GetAllGroupedAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateMail(MailSettingsViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = _localizer.T("Alert.Error.General");
            return RedirectToAction(nameof(Index), new { tab = "mail" });
        }

        await _settingsService.SetValueAsync("Smtp.Host", model.Smtp_Host);
        await _settingsService.SetValueAsync("Smtp.Port", model.Smtp_Port?.ToString());
        await _settingsService.SetValueAsync("Smtp.Username", model.Smtp_Username);
        await _settingsService.SetValueAsync("Smtp.Password", model.Smtp_Password);
        await _settingsService.SetValueAsync("Smtp.EnableSsl", model.Smtp_EnableSsl?.ToString());
        await _settingsService.SetValueAsync("Smtp.FromAddress", model.Smtp_FromAddress);
        await _settingsService.SetValueAsync("Smtp.FromName", model.Smtp_FromName);
        await _settingsService.SetValueAsync("Smtp.TestRecipientEmail", model.Smtp_TestRecipientEmail);

        TempData["SuccessMessage"] = _localizer.T("Settings.Mail.Updated");
        return RedirectToAction(nameof(Index), new { tab = "mail" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateDocumentTemplate(DocumentTemplateSettingsViewModel model)
    {
        await _settingsService.SetValueAsync("DocumentTemplate.DefaultExpiryWarningDays", model.DocumentTemplate_DefaultExpiryWarningDays?.ToString());
        await _settingsService.SetValueAsync("DocumentTemplate.AutoAssignToNewClinics", model.DocumentTemplate_AutoAssignToNewClinics?.ToString());
        await _settingsService.SetValueAsync("DocumentTemplate.AllowedFileExtensions", model.DocumentTemplate_AllowedFileExtensions);
        await _settingsService.SetValueAsync("DocumentTemplate.MaxFileSizeMB", model.DocumentTemplate_MaxFileSizeMB?.ToString());
        await _settingsService.SetValueAsync("DocumentTemplate.DefaultClinicTypeCategory", model.DocumentTemplate_DefaultClinicTypeCategory);

        TempData["SuccessMessage"] = _localizer.T("Settings.DocumentTemplate.Updated");
        return RedirectToAction(nameof(Index), new { tab = "documenttemplate" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateBranding(BrandingSettingsViewModel model)
    {
        await _settingsService.SetValueAsync("Branding.SiteName", model.Branding_SiteName);
        await _settingsService.SetValueAsync("Branding.SiteNameAr", model.Branding_SiteNameAr);
        await _settingsService.SetValueAsync("Branding.DefaultLogoPath", model.Branding_DefaultLogoPath);
        await _settingsService.SetValueAsync("Branding.PrimaryColor", model.Branding_PrimaryColor);
        await _settingsService.SetValueAsync("Branding.SupportPhone", model.Branding_SupportPhone);
        await _settingsService.SetValueAsync("Branding.SupportEmail", model.Branding_SupportEmail);

        TempData["SuccessMessage"] = _localizer.T("Settings.Branding.Updated");
        return RedirectToAction(nameof(Index), new { tab = "branding" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateNotifications(NotificationsSettingsViewModel model)
    {
        await _settingsService.SetValueAsync("Notifications.PolicyExpiryWarningDays", model.Notifications_PolicyExpiryWarningDays?.ToString());
        await _settingsService.SetValueAsync("Notifications.HRDocumentExpiryWarningDays", model.Notifications_HRDocumentExpiryWarningDays?.ToString());
        await _settingsService.SetValueAsync("Notifications.EnableEmailNotifications", model.Notifications_EnableEmailNotifications?.ToString());
        await _settingsService.SetValueAsync("Notifications.DailyDigestEnabled", model.Notifications_DailyDigestEnabled?.ToString());
        await _settingsService.SetValueAsync("Notifications.DigestSendTime", model.Notifications_DigestSendTime);

        TempData["SuccessMessage"] = _localizer.T("Settings.Notifications.Updated");
        return RedirectToAction(nameof(Index), new { tab = "notifications" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateLocalization(LocalizationSettingsViewModel model)
    {
        await _settingsService.SetValueAsync("Localization.DefaultLanguage", model.Localization_DefaultLanguage);
        await _settingsService.SetValueAsync("Localization.AllowUserLanguageToggle", model.Localization_AllowUserLanguageToggle?.ToString());

        TempData["SuccessMessage"] = _localizer.T("Settings.Localization.Updated");
        return RedirectToAction(nameof(Index), new { tab = "localization" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateGeneral(GeneralSettingsViewModel model)
    {
        await _settingsService.SetValueAsync("General.SessionTimeoutMinutes", model.General_SessionTimeoutMinutes?.ToString());
        await _settingsService.SetValueAsync("General.MaintenanceMode", model.General_MaintenanceMode?.ToString());
        await _settingsService.SetValueAsync("General.MaintenanceMessage", model.General_MaintenanceMessage);

        TempData["SuccessMessage"] = _localizer.T("Settings.General.Updated");
        return RedirectToAction(nameof(Index), new { tab = "general" });
    }

    [HttpPost]
    public async Task<IActionResult> SendTestEmail()
    {
        try
        {
            var testRecipient = await _settingsService.GetValueAsync("Smtp.TestRecipientEmail");
            if (string.IsNullOrEmpty(testRecipient))
            {
                return Json(new { success = false, message = _localizer.T("Settings.Mail.TestRecipientMissing") });
            }

            var (success, message) = await _emailService.SendTestEmailAsync(testRecipient);
            return Json(new { success, message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending test email");
            return Json(new { success = false, message = _localizer.T("Settings.TestEmailFailed") });
        }
    }
}
