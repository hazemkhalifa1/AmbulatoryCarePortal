using Microsoft.AspNetCore.Identity;
using AmbulatoryCarePortal.Domain.Enums;

namespace AmbulatoryCarePortal.Infrastructure.Data.Seed;

public static class RolePermissionsSeeder
{
    public static class Permissions
    {
        // Clinic Management Permissions
        public const string CreateClinic = "clinics.create";
        public const string ReadClinic = "clinics.read";
        public const string UpdateClinic = "clinics.update";
        public const string DeleteClinic = "clinics.delete";
        public const string ViewComplianceScore = "clinics.compliance.view";
        public const string ExportClinicData = "clinics.export";

        // Policy Permissions
        public const string CreatePolicy = "policies.create";
        public const string ReadPolicy = "policies.read";
        public const string UpdatePolicy = "policies.update";
        public const string DeletePolicy = "policies.delete";
        public const string UploadEvidence = "policies.evidence.upload";
        public const string ApprovePolicy = "policies.approve";

        // KPI Permissions
        public const string CreateKPI = "kpis.create";
        public const string ReadKPI = "kpis.read";
        public const string UpdateKPI = "kpis.update";
        public const string DeleteKPI = "kpis.delete";
        public const string EnterKPIData = "kpis.data.enter";
        public const string ExportKPIReport = "kpis.export";

        // Checklist Permissions
        public const string CreateChecklist = "checklists.create";
        public const string ReadChecklist = "checklists.read";
        public const string ExecuteChecklist = "checklists.execute";
        public const string ApproveChecklist = "checklists.approve";
        public const string ViewChecklistHistory = "checklists.history.view";

        // HR Permissions
        public const string ManageStaff = "staff.manage";
        public const string ViewStaff = "staff.view";
        public const string ManageDocuments = "documents.manage";
        public const string UploadDocuments = "documents.upload";
        public const string VerifyDocuments = "documents.verify";
        public const string ExpiryNotifications = "documents.expiry.view";

        // Audit & Compliance
        public const string ViewAuditLog = "audit.view";
        public const string ExportAuditLog = "audit.export";
        public const string ManageNotifications = "notifications.manage";
        public const string SendNotifications = "notifications.send";

        // User Management
        public const string ManageUsers = "users.manage";
        public const string CreateUser = "users.create";
        public const string EditUser = "users.edit";
        public const string DeleteUser = "users.delete";
        public const string ManageRoles = "roles.manage";

        // Dashboard & Reports
        public const string ViewDashboard = "dashboard.view";
        public const string GenerateReports = "reports.generate";
        public const string ExportReports = "reports.export";
        public const string ViewAnalytics = "analytics.view";

        // System Settings
        public const string ConfigureSystem = "system.configure";
        public const string ViewSystemSettings = "system.settings.view";
        public const string ManageEmailSettings = "system.email.manage";
        public const string BackupSystem = "system.backup";
    }

    public static async Task SeedRolesWithPermissionsAsync(RoleManager<IdentityRole> roleManager)
    {
        // SuperAdmin Role - Full system access
        await CreateOrUpdateRoleAsync(roleManager, "SuperAdmin", new[]
        {
            Permissions.CreateClinic, Permissions.ReadClinic, Permissions.UpdateClinic, Permissions.DeleteClinic,
            Permissions.ViewComplianceScore, Permissions.ExportClinicData,
            Permissions.CreatePolicy, Permissions.ReadPolicy, Permissions.UpdatePolicy, Permissions.DeletePolicy,
            Permissions.UploadEvidence, Permissions.ApprovePolicy,
            Permissions.CreateKPI, Permissions.ReadKPI, Permissions.UpdateKPI, Permissions.DeleteKPI,
            Permissions.EnterKPIData, Permissions.ExportKPIReport,
            Permissions.CreateChecklist, Permissions.ReadChecklist, Permissions.ExecuteChecklist,
            Permissions.ApproveChecklist, Permissions.ViewChecklistHistory,
            Permissions.ManageStaff, Permissions.ViewStaff, Permissions.ManageDocuments,
            Permissions.UploadDocuments, Permissions.VerifyDocuments, Permissions.ExpiryNotifications,
            Permissions.ViewAuditLog, Permissions.ExportAuditLog, Permissions.ManageNotifications,
            Permissions.SendNotifications, Permissions.ManageUsers, Permissions.CreateUser,
            Permissions.EditUser, Permissions.DeleteUser, Permissions.ManageRoles,
            Permissions.ViewDashboard, Permissions.GenerateReports, Permissions.ExportReports,
            Permissions.ViewAnalytics, Permissions.ConfigureSystem, Permissions.ViewSystemSettings,
            Permissions.ManageEmailSettings, Permissions.BackupSystem
        });

        // ClinicAdmin Role - Full clinic management
        await CreateOrUpdateRoleAsync(roleManager, "ClinicAdmin", new[]
        {
            Permissions.ReadClinic, Permissions.UpdateClinic, Permissions.ViewComplianceScore,
            Permissions.ExportClinicData, Permissions.CreatePolicy, Permissions.ReadPolicy,
            Permissions.UpdatePolicy, Permissions.UploadEvidence, Permissions.ApprovePolicy,
            Permissions.CreateKPI, Permissions.ReadKPI, Permissions.UpdateKPI, Permissions.EnterKPIData,
            Permissions.ExportKPIReport, Permissions.CreateChecklist, Permissions.ReadChecklist,
            Permissions.ExecuteChecklist, Permissions.ApproveChecklist, Permissions.ViewChecklistHistory,
            Permissions.ManageStaff, Permissions.ViewStaff, Permissions.ManageDocuments,
            Permissions.UploadDocuments, Permissions.VerifyDocuments, Permissions.ExpiryNotifications,
            Permissions.ViewAuditLog, Permissions.ManageUsers, Permissions.ViewDashboard,
            Permissions.GenerateReports, Permissions.ExportReports, Permissions.ViewAnalytics,
            Permissions.ViewSystemSettings
        });

        // DepartmentHead Role - Department management
        await CreateOrUpdateRoleAsync(roleManager, "DepartmentHead", new[]
        {
            Permissions.ReadClinic, Permissions.ViewComplianceScore, Permissions.ReadPolicy,
            Permissions.UpdatePolicy, Permissions.UploadEvidence, Permissions.CreateKPI,
            Permissions.ReadKPI, Permissions.UpdateKPI, Permissions.EnterKPIData,
            Permissions.CreateChecklist, Permissions.ReadChecklist, Permissions.ExecuteChecklist,
            Permissions.ViewChecklistHistory, Permissions.ViewStaff, Permissions.UploadDocuments,
            Permissions.ExpiryNotifications, Permissions.ViewAuditLog, Permissions.ViewDashboard,
            Permissions.GenerateReports
        });

        // DepartmentUser Role - Basic department operations
        await CreateOrUpdateRoleAsync(roleManager, "DepartmentUser", new[]
        {
            Permissions.ReadClinic, Permissions.ReadPolicy, Permissions.UploadEvidence,
            Permissions.ReadKPI, Permissions.EnterKPIData, Permissions.ReadChecklist,
            Permissions.ExecuteChecklist, Permissions.ViewStaff, Permissions.UploadDocuments,
            Permissions.ViewDashboard
        });

        // Auditor Role - Read-only audit access
        await CreateOrUpdateRoleAsync(roleManager, "Auditor", new[]
        {
            Permissions.ReadClinic, Permissions.ViewComplianceScore, Permissions.ReadPolicy,
            Permissions.ReadKPI, Permissions.ReadChecklist, Permissions.ViewChecklistHistory,
            Permissions.ViewStaff, Permissions.ViewAuditLog, Permissions.ExportAuditLog,
            Permissions.ViewDashboard, Permissions.GenerateReports, Permissions.ExportReports,
            Permissions.ViewAnalytics
        });

        // Viewer Role - Limited read-only access
        await CreateOrUpdateRoleAsync(roleManager, "Viewer", new[]
        {
            Permissions.ReadClinic, Permissions.ViewComplianceScore, Permissions.ReadPolicy,
            Permissions.ReadKPI, Permissions.ReadChecklist, Permissions.ViewDashboard
        });

        // HRManager Role - HR-specific management
        await CreateOrUpdateRoleAsync(roleManager, "HRManager", new[]
        {
            Permissions.ManageStaff, Permissions.ViewStaff, Permissions.ManageDocuments,
            Permissions.UploadDocuments, Permissions.VerifyDocuments, Permissions.ExpiryNotifications,
            Permissions.ViewAuditLog, Permissions.ViewDashboard, Permissions.GenerateReports,
            Permissions.ExportReports
        });

        // Compliance Officer Role - Compliance monitoring
        await CreateOrUpdateRoleAsync(roleManager, "ComplianceOfficer", new[]
        {
            Permissions.ReadClinic, Permissions.ViewComplianceScore, Permissions.ReadPolicy,
            Permissions.ApprovePolicy, Permissions.ReadKPI, Permissions.ReadChecklist,
            Permissions.ApproveChecklist, Permissions.ViewChecklistHistory, Permissions.ViewAuditLog,
            Permissions.ViewDashboard, Permissions.GenerateReports, Permissions.ExportReports,
            Permissions.ViewAnalytics
        });
    }

    private static async Task CreateOrUpdateRoleAsync(RoleManager<IdentityRole> roleManager, 
        string roleName, string[] permissions)
    {
        var role = await roleManager.FindByNameAsync(roleName);
        if (role == null)
        {
            role = new IdentityRole(roleName);
            await roleManager.CreateAsync(role);
        }
    }
}

/// <summary>
/// Role Descriptions for UI Display
/// </summary>
public static class RoleDescriptions
{
    public static readonly Dictionary<string, string> Descriptions = new()
    {
        {
            "SuperAdmin", 
            "Full system access. Manage clinics, users, roles, and system configuration. View all reports and audit logs."
        },
        {
            "ClinicAdmin",
            "Manage clinic operations including policies, KPIs, checklists, staff, and compliance monitoring. Generate clinic reports."
        },
        {
            "DepartmentHead",
            "Manage department policies, KPIs, and checklists. Supervise department staff and monitor compliance within the department."
        },
        {
            "DepartmentUser",
            "Execute checklists, enter KPI data, upload documents, and support department operations."
        },
        {
            "Auditor",
            "View-only access to audit logs, compliance data, and system reports. No modification permissions."
        },
        {
            "Viewer",
            "Limited read-only access to clinic dashboard and basic compliance information."
        },
        {
            "HRManager",
            "Manage HR staff records, employee documents, and expiry notifications. Monitor HR compliance."
        },
        {
            "ComplianceOfficer",
            "Monitor and enforce compliance standards. Approve policies and checklists. Generate compliance reports."
        }
    };

    public static readonly Dictionary<string, string[]> ResponsibilitiesByRole = new()
    {
        {
            "SuperAdmin", new[]
            {
                "System administration and configuration",
                "Clinic management and approval",
                "User and role management",
                "System-wide compliance oversight",
                "Backup and system maintenance",
                "Email and notification settings"
            }
        },
        {
            "ClinicAdmin", new[]
            {
                "Clinic operations management",
                "Staff and document management",
                "Policy and checklist administration",
                "KPI definition and monitoring",
                "User management within clinic",
                "Clinic compliance monitoring"
            }
        },
        {
            "DepartmentHead", new[]
            {
                "Department operations oversight",
                "Staff supervision",
                "Policy implementation within department",
                "KPI monitoring and reporting",
                "Checklist execution oversight",
                "Document management"
            }
        },
        {
            "DepartmentUser", new[]
            {
                "Daily checklist execution",
                "KPI data entry",
                "Document uploads",
                "Policy compliance",
                "Team support",
                "Evidence documentation"
            }
        },
        {
            "Auditor", new[]
            {
                "Audit trail review",
                "Compliance verification",
                "Report generation",
                "System monitoring",
                "Documentation review",
                "No modifications allowed"
            }
        },
        {
            "Viewer", new[]
            {
                "Dashboard viewing",
                "Basic compliance information access",
                "Report reading",
                "No modification permissions"
            }
        },
        {
            "HRManager", new[]
            {
                "Staff record management",
                "Document verification",
                "Expiry monitoring",
                "HR compliance oversight",
                "Staff onboarding",
                "Certification tracking"
            }
        },
        {
            "ComplianceOfficer", new[]
            {
                "Compliance monitoring",
                "Policy approval",
                "Checklist approval",
                "Standards enforcement",
                "Report generation",
                "Compliance recommendations"
            }
        }
    };
}
