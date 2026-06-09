using System.Security.Claims;

namespace AmbulatoryCarePortal.Presentation.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static string GetUserId(this ClaimsPrincipal principal)
    {
        return principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
    }

    public static string GetEmail(this ClaimsPrincipal principal)
    {
        return principal.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;
    }

    public static string GetFullName(this ClaimsPrincipal principal)
    {
        return principal.FindFirst("FullNameEn")?.Value ?? principal.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;
    }

    public static string GetFullNameAr(this ClaimsPrincipal principal)
    {
        return principal.FindFirst("FullNameAr")?.Value ?? string.Empty;
    }

    public static int? GetClinicId(this ClaimsPrincipal principal)
    {
        var clinicId = principal.FindFirst("ClinicId")?.Value;
        return int.TryParse(clinicId, out var id) ? id : null;
    }

    public static List<string> GetRoles(this ClaimsPrincipal principal)
    {
        return principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
    }

    public static bool HasRole(this ClaimsPrincipal principal, string role)
    {
        return principal.IsInRole(role);
    }

    public static bool IsSuperAdmin(this ClaimsPrincipal principal)
    {
        return principal.HasRole("SuperAdmin");
    }

    public static bool IsClinicAdmin(this ClaimsPrincipal principal)
    {
        return principal.HasRole("ClinicAdmin");
    }

    public static bool IsClinicViewer(this ClaimsPrincipal principal)
    {
        return principal.HasRole("ClinicViewer");
    }

    public static bool HasPermission(this ClaimsPrincipal principal, string permission)
    {
        return principal.HasClaim("Permission", permission);
    }
}
