using AmbulatoryCarePortal.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace AmbulatoryCarePortal.Presentation.ViewModels;

public class MailSettingsViewModel
{
    public string? Smtp_Host { get; set; }
    public int? Smtp_Port { get; set; }
    public string? Smtp_Username { get; set; }
    public string? Smtp_Password { get; set; }
    public bool? Smtp_EnableSsl { get; set; }
    public string? Smtp_FromAddress { get; set; }
    public string? Smtp_FromName { get; set; }
    public string? Smtp_TestRecipientEmail { get; set; }
}

public class DocumentTemplateSettingsViewModel
{
    public int? DocumentTemplate_DefaultExpiryWarningDays { get; set; }
    public bool? DocumentTemplate_AutoAssignToNewClinics { get; set; }
    public string? DocumentTemplate_AllowedFileExtensions { get; set; }
    public int? DocumentTemplate_MaxFileSizeMB { get; set; }
    public string? DocumentTemplate_DefaultClinicTypeCategory { get; set; }
}

public class BrandingSettingsViewModel
{
    public string? Branding_SiteName { get; set; }
    public string? Branding_SiteNameAr { get; set; }
    public string? Branding_DefaultLogoPath { get; set; }
    public string? Branding_PrimaryColor { get; set; }
    public string? Branding_SupportPhone { get; set; }
    public string? Branding_SupportEmail { get; set; }
}

public class NotificationsSettingsViewModel
{
    public int? Notifications_PolicyExpiryWarningDays { get; set; }
    public int? Notifications_HRDocumentExpiryWarningDays { get; set; }
    public bool? Notifications_EnableEmailNotifications { get; set; }
    public bool? Notifications_DailyDigestEnabled { get; set; }
    public string? Notifications_DigestSendTime { get; set; }
}

public class LocalizationSettingsViewModel
{
    public string? Localization_DefaultLanguage { get; set; }
    public bool? Localization_AllowUserLanguageToggle { get; set; }
}

public class GeneralSettingsViewModel
{
    public int? General_SessionTimeoutMinutes { get; set; }
    public bool? General_MaintenanceMode { get; set; }
    public string? General_MaintenanceMessage { get; set; }
}
