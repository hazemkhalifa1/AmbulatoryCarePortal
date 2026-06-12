using System.Security.Claims;
using AmbulatoryCarePortal.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace AmbulatoryCarePortal.Presentation.Helpers;

public class ClinicClaimsPrincipalFactory : UserClaimsPrincipalFactory<AppUser, IdentityRole>
{
    public ClinicClaimsPrincipalFactory(
        UserManager<AppUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IOptions<IdentityOptions> optionsAccessor)
        : base(userManager, roleManager, optionsAccessor)
    {
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(AppUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        if (user.ClinicId.HasValue)
        {
            identity.AddClaim(new Claim("ClinicId", user.ClinicId.Value.ToString()));
        }

        return identity;
    }
}
