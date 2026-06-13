using Microsoft.AspNetCore.Identity;
using AmbulatoryCarePortal.Domain.Entities;
using AmbulatoryCarePortal.Infrastructure.Data;
using AmbulatoryCarePortal.Presentation.Authorization;
using AmbulatoryCarePortal.Presentation.Helpers;

namespace AmbulatoryCarePortal.Presentation.DependencyInjection;

public static class IdentityServiceExtensions
{
    public static IServiceCollection AddIdentityConfiguration(this IServiceCollection services)
    {
        services.AddIdentity<AppUser, IdentityRole>(options =>
        {
            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);

            options.SignIn.RequireConfirmedEmail = true;
            options.SignIn.RequireConfirmedAccount = true;
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        services.AddScoped<IUserClaimsPrincipalFactory<AppUser>, ClinicClaimsPrincipalFactory>();

        services.AddAuthorization(options =>
        {
            options.AddPermissionPolicies();
        });

        services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromMinutes(20);
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        });

        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.SlidingExpiration = true;
            options.LoginPath = "/Account/Login";
            options.LogoutPath = "/Account/Logout";
            options.AccessDeniedPath = "/Account/AccessDenied";
        });

        return services;
    }
}
