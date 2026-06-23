using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace AmbulatoryCarePortal.Infrastructure.Data.Seed;

public static class RolePermissionsSeeder
{
    public static class Permissions
    {
        // Clinic Management
        public const string CreateClinic = "clinics.create";
        public const string ReadClinic = "clinics.read";
        public const string UpdateClinic = "clinics.update";
        public const string DeleteClinic = "clinics.delete";
        public const string ViewComplianceScore = "clinics.compliance.view";
        public const string ExportClinicData = "clinics.export";

        // Policy
        public const string CreatePolicy = "policies.create";
        public const string ReadPolicy = "policies.read";
        public const string UpdatePolicy = "policies.update";
        public const string DeletePolicy = "policies.delete";
        public const string UploadEvidence = "policies.evidence.upload";
        public const string ApprovePolicy = "policies.approve";

        // KPI
        public const string CreateKPI = "kpis.create";
        public const string ReadKPI = "kpis.read";
        public const string UpdateKPI = "kpis.update";
        public const string DeleteKPI = "kpis.delete";
        public const string EnterKPIData = "kpis.data.enter";
        public const string ExportKPIReport = "kpis.export";

        // Checklist
        public const string CreateChecklist = "checklists.create";
        public const string ReadChecklist = "checklists.read";
        public const string ExecuteChecklist = "checklists.execute";
        public const string ApproveChecklist = "checklists.approve";
        public const string ViewChecklistHistory = "checklists.history.view";

        // HR
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

        // Signature Management
        public const string ManageSignatures = "signatures.manage";
        public const string ViewSignatures = "signatures.view";
    }

    public static async Task SeedRolesWithPermissionsAsync(RoleManager<IdentityRole> roleManager)
    {
        await CreateOrUpdateRoleWithPermissionsAsync(roleManager, "SuperAdmin", GetAllPermissions());
        await CreateOrUpdateRoleWithPermissionsAsync(roleManager, "ClinicAdmin", GetClinicAdminPermissions());
        await CreateOrUpdateRoleWithPermissionsAsync(roleManager, "ClinicViewer", GetClinicViewerPermissions());
    }

    private static async Task CreateOrUpdateRoleWithPermissionsAsync(
        RoleManager<IdentityRole> roleManager,
        string roleName,
        string[] permissions)
    {
        var role = await roleManager.FindByNameAsync(roleName);
        if (role == null)
        {
            role = new IdentityRole(roleName);
            await roleManager.CreateAsync(role);
        }

        var existingClaims = await roleManager.GetClaimsAsync(role);
        var permissionClaims = existingClaims.Where(c => c.Type == "Permission").ToList();

        foreach (var claim in permissionClaims)
        {
            if (!permissions.Contains(claim.Value))
            {
                await roleManager.RemoveClaimAsync(role, claim);
            }
        }

        foreach (var permission in permissions)
        {
            if (!permissionClaims.Any(c => c.Value == permission))
            {
                await roleManager.AddClaimAsync(role, new Claim("Permission", permission));
            }
        }
    }

    private static string[] GetAllPermissions() => new[]
    {
        Permissions.CreateClinic, Permissions.ReadClinic,
        Permissions.UpdateClinic, Permissions.DeleteClinic,
        Permissions.ViewComplianceScore, Permissions.ExportClinicData,
        Permissions.CreatePolicy, Permissions.ReadPolicy,
        Permissions.UpdatePolicy, Permissions.DeletePolicy,
        Permissions.UploadEvidence, Permissions.ApprovePolicy,
        Permissions.CreateKPI, Permissions.ReadKPI,
        Permissions.UpdateKPI, Permissions.DeleteKPI,
        Permissions.EnterKPIData, Permissions.ExportKPIReport,
        Permissions.CreateChecklist, Permissions.ReadChecklist,
        Permissions.ExecuteChecklist, Permissions.ApproveChecklist,
        Permissions.ViewChecklistHistory,
        Permissions.ManageStaff, Permissions.ViewStaff,
        Permissions.ManageDocuments, Permissions.UploadDocuments,
        Permissions.VerifyDocuments, Permissions.ExpiryNotifications,
        Permissions.ViewAuditLog, Permissions.ExportAuditLog,
        Permissions.ManageNotifications, Permissions.SendNotifications,
        Permissions.ManageUsers, Permissions.CreateUser,
        Permissions.EditUser, Permissions.DeleteUser, Permissions.ManageRoles,
        Permissions.ViewDashboard, Permissions.GenerateReports,
        Permissions.ExportReports, Permissions.ViewAnalytics,
        Permissions.ConfigureSystem, Permissions.ViewSystemSettings,
        Permissions.ManageEmailSettings, Permissions.BackupSystem,
        Permissions.ManageSignatures, Permissions.ViewSignatures
    };

    private static string[] GetClinicAdminPermissions() => new[]
    {
        Permissions.ReadClinic, Permissions.UpdateClinic,
        Permissions.ViewComplianceScore, Permissions.ExportClinicData,
        Permissions.CreatePolicy, Permissions.ReadPolicy,
        Permissions.UpdatePolicy, Permissions.UploadEvidence,
        Permissions.ApprovePolicy,
        Permissions.CreateKPI, Permissions.ReadKPI,
        Permissions.UpdateKPI, Permissions.EnterKPIData,
        Permissions.ExportKPIReport,
        Permissions.CreateChecklist, Permissions.ReadChecklist,
        Permissions.ExecuteChecklist, Permissions.ApproveChecklist,
        Permissions.ViewChecklistHistory,
        Permissions.ManageStaff, Permissions.ViewStaff,
        Permissions.ManageDocuments, Permissions.UploadDocuments,
        Permissions.VerifyDocuments, Permissions.ExpiryNotifications,
        Permissions.ViewAuditLog,
        Permissions.ManageNotifications, Permissions.SendNotifications,
        Permissions.ViewDashboard, Permissions.GenerateReports,
        Permissions.ExportReports, Permissions.ViewAnalytics,
        Permissions.ViewSystemSettings,
        Permissions.ManageSignatures,
        Permissions.ViewSignatures
    };

    private static string[] GetClinicViewerPermissions() => new[]
    {
        Permissions.ReadClinic,
        Permissions.ViewComplianceScore,
        Permissions.ReadPolicy,
        Permissions.ReadKPI,
        Permissions.ReadChecklist,
        Permissions.ViewChecklistHistory,
        Permissions.ViewStaff,
        Permissions.ViewDashboard,
        Permissions.ViewAnalytics,
        Permissions.ViewAuditLog,
        Permissions.ViewSignatures
    };
}

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
            "ClinicViewer",
            "Read-only access to clinic dashboard, compliance scores, policies, KPIs, checklists, and staff information. No modification permissions."
        }
    };

    public static readonly Dictionary<string, string[]> ResponsibilitiesByRole = new()
    {
        {
            "SuperAdmin", new[]
            {
                "System administration and configuration",
                "Clinic management (create, update, delete)",
                "User and role management",
                "System-wide compliance oversight",
                "Backup and system maintenance",
                "Email and notification settings"
            }
        },
        {
            "ClinicAdmin", new[]
            {
                "Clinic operations management (update clinic details)",
                "Staff and document management",
                "Policy and checklist administration (create, update, approve)",
                "KPI definition and monitoring",
                "Clinic compliance monitoring",
                "Report generation"
            }
        },
        {
            "ClinicViewer", new[]
            {
                "Dashboard viewing",
                "Read-only access to compliance data",
                "View policies, KPIs, checklists, and staff",
                "No modifications allowed"
            }
        }
    };
}
