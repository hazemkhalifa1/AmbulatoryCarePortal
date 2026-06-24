using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AmbulatoryCarePortal.Domain.Entities;
using AmbulatoryCarePortal.Infrastructure.Data;

namespace AmbulatoryCarePortal.Infrastructure.Data.Seed;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider, string adminPassword)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        // Apply migrations
        await dbContext.Database.MigrateAsync();

        // Seed roles
        await SeedRolesAsync(roleManager);

        // Seed admin user
        await SeedAdminUserAsync(userManager, dbContext, adminPassword);

        // Seed initial clinics and departments
        await DepartmentSeeder.SeedDepartmentsAsync(dbContext, "admin@cbahi-portal.com");

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        await RolePermissionsSeeder.SeedRolesWithPermissionsAsync(roleManager);
    }

    private static async Task SeedAdminUserAsync(UserManager<AppUser> userManager, AppDbContext dbContext, string adminPassword)
    {
        var adminEmail = "admin@cbahi-portal.com";
        var existingUser = await userManager.FindByEmailAsync(adminEmail);

        if (existingUser == null)
        {
            var adminUser = new AppUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullNameEn = "System Administrator",
                FullNameAr = "مسؤول النظام",
                EmailConfirmed = true,
                IsActive = true,
                // CreatedAt is set by BaseEntity default, not on AppUser (IdentityUser)
            };

            var result = await userManager.CreateAsync(adminUser, adminPassword);

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "SuperAdmin");
            }
        }
    }

}
