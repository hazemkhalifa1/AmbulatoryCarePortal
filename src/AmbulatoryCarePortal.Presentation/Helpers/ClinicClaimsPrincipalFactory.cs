using System.Security.Claims;
using System.Text.Json;
using AmbulatoryCarePortal.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace AmbulatoryCarePortal.Presentation.Helpers;

public class ClinicClaimsPrincipalFactory : UserClaimsPrincipalFactory<AppUser, IdentityRole>
{
    private readonly IDistributedCache _cache;

    public ClinicClaimsPrincipalFactory(
        UserManager<AppUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IOptions<IdentityOptions> optionsAccessor,
        IDistributedCache cache)
        : base(userManager, roleManager, optionsAccessor)
    {
        _cache = cache;
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(AppUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        if (user.ClinicId.HasValue)
        {
            identity.AddClaim(new Claim("ClinicId", user.ClinicId.Value.ToString()));
        }

        var roles = await UserManager.GetRolesAsync(user);
        foreach (var roleName in roles)
        {
            var permissionClaims = await GetCachedPermissionClaimsAsync(roleName);
            foreach (var claim in permissionClaims)
            {
                if (!identity.HasClaim(c => c.Type == claim.Type && c.Value == claim.Value))
                {
                    identity.AddClaim(claim);
                }
            }
        }

        return identity;
    }

    private async Task<List<Claim>> GetCachedPermissionClaimsAsync(string roleName)
    {
        var cacheKey = $"permissions:role:{roleName}";
        var cached = await _cache.GetStringAsync(cacheKey);
        if (cached != null)
        {
            var values = JsonSerializer.Deserialize<List<string>>(cached);
            if (values != null)
                return values.Select(v => new Claim("Permission", v)).ToList();
        }

        var role = await RoleManager.FindByNameAsync(roleName);
        if (role == null)
            return [];

        var claims = await RoleManager.GetClaimsAsync(role);
        var permissionClaims = claims.Where(c => c.Type == "Permission").ToList();

        var cacheValues = permissionClaims.Select(c => c.Value).ToList();
        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(cacheValues), new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
        });

        return permissionClaims;
    }
}
