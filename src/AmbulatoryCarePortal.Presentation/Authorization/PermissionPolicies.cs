using Microsoft.AspNetCore.Authorization;
using static AmbulatoryCarePortal.Infrastructure.Data.Seed.RolePermissionsSeeder;

namespace AmbulatoryCarePortal.Presentation.Authorization;

public static class PermissionPolicies
{
    public const string PolicyPrefix = "Permission";

    public static string PolicyName(string permission) => $"{PolicyPrefix}.{permission}";

    public static void AddPermissionPolicies(this AuthorizationOptions options)
    {
        options.AddPolicy(PolicyName(Permissions.CreateClinic), p =>
            p.Requirements.Add(new PermissionRequirement(Permissions.CreateClinic)));
        options.AddPolicy(PolicyName(Permissions.ReadClinic), p =>
            p.Requirements.Add(new PermissionRequirement(Permissions.ReadClinic)));
        options.AddPolicy(PolicyName(Permissions.UpdateClinic), p =>
            p.Requirements.Add(new PermissionRequirement(Permissions.UpdateClinic)));
        options.AddPolicy(PolicyName(Permissions.DeleteClinic), p =>
            p.Requirements.Add(new PermissionRequirement(Permissions.DeleteClinic)));
        options.AddPolicy(PolicyName(Permissions.ViewComplianceScore), p =>
            p.Requirements.Add(new PermissionRequirement(Permissions.ViewComplianceScore)));
        options.AddPolicy(PolicyName(Permissions.ExportClinicData), p =>
            p.Requirements.Add(new PermissionRequirement(Permissions.ExportClinicData)));

        options.AddPolicy(PolicyName(Permissions.CreatePolicy), p =>
            p.Requirements.Add(new PermissionRequirement(Permissions.CreatePolicy)));
        options.AddPolicy(PolicyName(Permissions.ReadPolicy), p =>
            p.Requirements.Add(new PermissionRequirement(Permissions.ReadPolicy)));
        options.AddPolicy(PolicyName(Permissions.UpdatePolicy), p =>
            p.Requirements.Add(new PermissionRequirement(Permissions.UpdatePolicy)));
        options.AddPolicy(PolicyName(Permissions.DeletePolicy), p =>
            p.Requirements.Add(new PermissionRequirement(Permissions.DeletePolicy)));
        options.AddPolicy(PolicyName(Permissions.UploadEvidence), p =>
            p.Requirements.Add(new PermissionRequirement(Permissions.UploadEvidence)));
        options.AddPolicy(PolicyName(Permissions.ApprovePolicy), p =>
            p.Requirements.Add(new PermissionRequirement(Permissions.ApprovePolicy)));

        options.AddPolicy(PolicyName(Permissions.CreateKPI), p =>
            p.Requirements.Add(new PermissionRequirement(Permissions.CreateKPI)));
        options.AddPolicy(PolicyName(Permissions.ReadKPI), p =>
            p.Requirements.Add(new PermissionRequirement(Permissions.ReadKPI)));
        options.AddPolicy(PolicyName(Permissions.UpdateKPI), p =>
            p.Requirements.Add(new PermissionRequirement(Permissions.UpdateKPI)));
        options.AddPolicy(PolicyName(Permissions.DeleteKPI), p =>
            p.Requirements.Add(new PermissionRequirement(Permissions.DeleteKPI)));
        options.AddPolicy(PolicyName(Permissions.EnterKPIData), p =>
            p.Requirements.Add(new PermissionRequirement(Permissions.EnterKPIData)));
        options.AddPolicy(PolicyName(Permissions.ExportKPIReport), p =>
            p.Requirements.Add(new PermissionRequirement(Permissions.ExportKPIReport)));

        options.AddPolicy(PolicyName(Permissions.CreateChecklist), p =>
            p.Requirements.Add(new PermissionRequirement(Permissions.CreateChecklist)));
        options.AddPolicy(PolicyName(Permissions.ReadChecklist), p =>
            p.Requirements.Add(new PermissionRequirement(Permissions.ReadChecklist)));
        options.AddPolicy(PolicyName(Permissions.ExecuteChecklist), p =>
            p.Requirements.Add(new PermissionRequirement(Permissions.ExecuteChecklist)));
        options.AddPolicy(PolicyName(Permissions.ApproveChecklist), p =>
            p.Requirements.Add(new PermissionRequirement(Permissions.ApproveChecklist)));
        options.AddPolicy(PolicyName(Permissions.ViewChecklistHistory), p =>
            p.Requirements.Add(new PermissionRequirement(Permissions.ViewChecklistHistory)));

        options.AddPolicy(PolicyName(Permissions.ManageStaff), p =>
            p.Requirements.Add(new PermissionRequirement(Permissions.ManageStaff)));
        options.AddPolicy(PolicyName(Permissions.ViewStaff), p =>
            p.Requirements.Add(new PermissionRequirement(Permissions.ViewStaff)));
        options.AddPolicy(PolicyName(Permissions.ManageDocuments), p =>
            p.Requirements.Add(new PermissionRequirement(Permissions.ManageDocuments)));
        options.AddPolicy(PolicyName(Permissions.UploadDocuments), p =>
            p.Requirements.Add(new PermissionRequirement(Permissions.UploadDocuments)));
        options.AddPolicy(PolicyName(Permissions.VerifyDocuments), p =>
            p.Requirements.Add(new PermissionRequirement(Permissions.VerifyDocuments)));
        options.AddPolicy(PolicyName(Permissions.ExpiryNotifications), p =>
            p.Requirements.Add(new PermissionRequirement(Permissions.ExpiryNotifications)));

        options.AddPolicy(PolicyName(Permissions.ViewAuditLog), p =>
            p.Requirements.Add(new PermissionRequirement(Permissions.ViewAuditLog)));
        options.AddPolicy(PolicyName(Permissions.ExportAuditLog), p =>
            p.Requirements.Add(new PermissionRequirement(Permissions.ExportAuditLog)));
        options.AddPolicy(PolicyName(Permissions.ManageNotifications), p =>
            p.Requirements.Add(new PermissionRequirement(Permissions.ManageNotifications)));
        options.AddPolicy(PolicyName(Permissions.SendNotifications), p =>
            p.Requirements.Add(new PermissionRequirement(Permissions.SendNotifications)));

        options.AddPolicy(PolicyName(Permissions.ManageUsers), p =>
            p.Requirements.Add(new PermissionRequirement(Permissions.ManageUsers)));
        options.AddPolicy(PolicyName(Permissions.CreateUser), p =>
            p.Requirements.Add(new PermissionRequirement(Permissions.CreateUser)));
        options.AddPolicy(PolicyName(Permissions.EditUser), p =>
            p.Requirements.Add(new PermissionRequirement(Permissions.EditUser)));
        options.AddPolicy(PolicyName(Permissions.DeleteUser), p =>
            p.Requirements.Add(new PermissionRequirement(Permissions.DeleteUser)));
        options.AddPolicy(PolicyName(Permissions.ManageRoles), p =>
            p.Requirements.Add(new PermissionRequirement(Permissions.ManageRoles)));

        options.AddPolicy(PolicyName(Permissions.ViewDashboard), p =>
            p.Requirements.Add(new PermissionRequirement(Permissions.ViewDashboard)));
        options.AddPolicy(PolicyName(Permissions.GenerateReports), p =>
            p.Requirements.Add(new PermissionRequirement(Permissions.GenerateReports)));
        options.AddPolicy(PolicyName(Permissions.ExportReports), p =>
            p.Requirements.Add(new PermissionRequirement(Permissions.ExportReports)));
        options.AddPolicy(PolicyName(Permissions.ViewAnalytics), p =>
            p.Requirements.Add(new PermissionRequirement(Permissions.ViewAnalytics)));

        options.AddPolicy(PolicyName(Permissions.ConfigureSystem), p =>
            p.Requirements.Add(new PermissionRequirement(Permissions.ConfigureSystem)));
        options.AddPolicy(PolicyName(Permissions.ViewSystemSettings), p =>
            p.Requirements.Add(new PermissionRequirement(Permissions.ViewSystemSettings)));
        options.AddPolicy(PolicyName(Permissions.ManageEmailSettings), p =>
            p.Requirements.Add(new PermissionRequirement(Permissions.ManageEmailSettings)));
        options.AddPolicy(PolicyName(Permissions.BackupSystem), p =>
            p.Requirements.Add(new PermissionRequirement(Permissions.BackupSystem)));
    }
}
