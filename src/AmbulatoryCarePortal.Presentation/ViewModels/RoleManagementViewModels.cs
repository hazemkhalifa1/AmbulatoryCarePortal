using AmbulatoryCarePortal.Domain.Entities;

namespace AmbulatoryCarePortal.Presentation.ViewModels;

/// <summary>
/// View Model for creating/editing users with role assignment
/// </summary>
public class UserRoleManagementViewModel
{
    public string UserId { get; set; }
    public string Email { get; set; }
    public string FullName { get; set; }
    public string PhoneNumber { get; set; }
    public bool IsActive { get; set; }
    public int? ClinicId { get; set; }
    public int? DepartmentId { get; set; }
    public string SelectedRole { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    
    public List<RoleOptionViewModel> AvailableRoles { get; set; } = new();
    public List<ClinicViewModel> AvailableClinics { get; set; } = new();
    public List<DepartmentViewModel> AvailableDepartments { get; set; } = new();
}

public class RoleOptionViewModel
{
    public string RoleName { get; set; }
    public string Description { get; set; }
    public string[] Permissions { get; set; }
    public string[] Responsibilities { get; set; }
    public bool IsAssigned { get; set; }
    public int PermissionCount { get; set; }
}

public class ClinicViewModel
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string NameAr { get; set; }
    public string CityEn { get; set; }
}

public class DepartmentViewModel
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int ClinicId { get; set; }
}

/// <summary>
/// Dashboard ViewModel for different role types
/// </summary>
public class RoleBasedDashboardViewModel
{
    public string UserRole { get; set; }
    public DashboardMetricsViewModel Metrics { get; set; }
    public List<RecentActivityViewModel> RecentActivities { get; set; }
    public List<PendingTaskViewModel> PendingTasks { get; set; }
    public ComplianceOverviewViewModel ComplianceOverview { get; set; }
    public string[] AccessibleFeatures { get; set; }
}

public class DashboardMetricsViewModel
{
    public int TotalClinics { get; set; }
    public int ActiveUsers { get; set; }
    public decimal AverageCompliance { get; set; }
    public int PendingApprovals { get; set; }
    public int OverdueItems { get; set; }
    public int ExpiringDocuments { get; set; }
}

public class RecentActivityViewModel
{
    public int Id { get; set; }
    public string Action { get; set; }
    public string User { get; set; }
    public string Target { get; set; }
    public DateTime Timestamp { get; set; }
    public string Description { get; set; }
}

public class PendingTaskViewModel
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Type { get; set; }
    public DateTime DueDate { get; set; }
    public string Priority { get; set; }
    public string AssignedTo { get; set; }
}

public class ComplianceOverviewViewModel
{
    public decimal OverallScore { get; set; }
    public int TotalPolicies { get; set; }
    public int ApprovedPolicies { get; set; }
    public int PendingPolicies { get; set; }
    public decimal PolicyCompletionRate { get; set; }
    public int TotalChecklists { get; set; }
    public int CompletedChecklists { get; set; }
    public decimal ChecklistCompletionRate { get; set; }
}

/// <summary>
/// Professional Features View Model
/// </summary>
public class ProfessionalFeaturesViewModel
{
    public List<FeatureCategoryViewModel> Categories { get; set; }
}

public class FeatureCategoryViewModel
{
    public string CategoryName { get; set; }
    public string Icon { get; set; }
    public List<FeatureDetailViewModel> Features { get; set; }
}

public class FeatureDetailViewModel
{
    public string FeatureName { get; set; }
    public string Description { get; set; }
    public string Icon { get; set; }
    public string[] RequiredRoles { get; set; }
    public bool IsAvailable { get; set; }
}

/// <summary>
/// Export and Reporting ViewModel
/// </summary>
public class ReportGeneratorViewModel
{
    public string ReportType { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int[] SelectedClinicIds { get; set; }
    public int[] SelectedDepartmentIds { get; set; }
    public string Format { get; set; } // PDF, Excel, CSV
    public bool IncludeCharts { get; set; }
    public bool IncludeAuditTrail { get; set; }
    public List<ReportTypeOptionViewModel> AvailableReportTypes { get; set; }
}

public class ReportTypeOptionViewModel
{
    public string Type { get; set; }
    public string DisplayName { get; set; }
    public string Description { get; set; }
    public string[] SupportedFormats { get; set; }
    public string[] RequiredRoles { get; set; }
}

/// <summary>
/// Analytics and Insights ViewModel
/// </summary>
public class AnalyticsViewModel
{
    public string AnalyticsType { get; set; }
    public List<ChartDataViewModel> ChartData { get; set; }
    public List<InsightViewModel> Insights { get; set; }
    public List<TrendViewModel> Trends { get; set; }
}

public class ChartDataViewModel
{
    public string ChartType { get; set; } // Line, Bar, Pie, etc.
    public string Title { get; set; }
    public object[] Labels { get; set; }
    public object[] Values { get; set; }
}

public class InsightViewModel
{
    public string Title { get; set; }
    public string Description { get; set; }
    public string Type { get; set; } // Success, Warning, Info, Danger
    public string Recommendation { get; set; }
}

public class TrendViewModel
{
    public string Metric { get; set; }
    public decimal ChangePercentage { get; set; }
    public string Direction { get; set; } // Up, Down, Stable
    public string Period { get; set; }
}

/// <summary>
/// System Administration ViewModel
/// </summary>
public class SystemAdministrationViewModel
{
    public SystemSettingsViewModel Settings { get; set; }
    public List<AuditLogViewModel> RecentAuditLogs { get; set; }
    public SystemHealthViewModel SystemHealth { get; set; }
}

public class SystemSettingsViewModel
{
    public string EmailProvider { get; set; }
    public string SMTPServer { get; set; }
    public int SMTPPort { get; set; }
    public bool EnableSSL { get; set; }
    public int MaxFileUploadSize { get; set; }
    public int SessionTimeout { get; set; }
    public bool TwoFactorAuthEnabled { get; set; }
    public int PasswordMinLength { get; set; }
    public bool RequireSpecialCharacters { get; set; }
}

public class AuditLogViewModel
{
    public int Id { get; set; }
    public string Action { get; set; }
    public string TargetType { get; set; }
    public string User { get; set; }
    public DateTime ActionDate { get; set; }
    public string IpAddress { get; set; }
    public string OldValues { get; set; }
    public string NewValues { get; set; }
}

public class SystemHealthViewModel
{
    public bool DatabaseConnected { get; set; }
    public bool EmailServiceWorking { get; set; }
    public decimal DiskUsagePercentage { get; set; }
    public int ActiveSessions { get; set; }
    public double AverageResponseTime { get; set; }
    public int TotalRequests { get; set; }
    public int ErrorCount { get; set; }
}

/// <summary>
/// Notification and Alert Settings ViewModel
/// </summary>
public class NotificationSettingsViewModel
{
    public bool EmailNotificationsEnabled { get; set; }
    public bool SMSNotificationsEnabled { get; set; }
    public bool InAppNotificationsEnabled { get; set; }
    public bool DailyDigestEnabled { get; set; }
    public List<NotificationPreferenceViewModel> Preferences { get; set; }
}

public class NotificationPreferenceViewModel
{
    public string NotificationType { get; set; }
    public bool EmailEnabled { get; set; }
    public bool SMSEnabled { get; set; }
    public bool InAppEnabled { get; set; }
    public string Frequency { get; set; } // Immediate, Daily, Weekly
}
