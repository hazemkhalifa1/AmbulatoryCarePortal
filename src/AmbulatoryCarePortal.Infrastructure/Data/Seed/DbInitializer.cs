using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AmbulatoryCarePortal.Domain.Entities;
using AmbulatoryCarePortal.Domain.Enums;
using AmbulatoryCarePortal.Infrastructure.Data;

namespace AmbulatoryCarePortal.Infrastructure.Data.Seed;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
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
        await SeedAdminUserAsync(userManager, dbContext);

        // Seed initial clinics and departments
        await SeedInitialDataAsync(dbContext);

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        var roles = new[] { "SuperAdmin", "HospitalAdmin", "ClinicAdmin", "DepartmentHead", "DepartmentUser", "ComplianceOfficer", "HRManager", "Auditor", "Viewer" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }

    private static async Task SeedAdminUserAsync(UserManager<AppUser> userManager, AppDbContext dbContext)
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

            var result = await userManager.CreateAsync(adminUser, "CbahiAdmin@2024");

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "SuperAdmin");
            }
        }
    }

    private static async Task SeedInitialDataAsync(AppDbContext dbContext)
    {
        // Check if data already seeded
        if (await dbContext.Clinics.AnyAsync())
            return;

        // Seed initial clinic
        var clinic = new Clinic
        {
            Name = "Demo Clinic",
            NameAr = "العيادة التجريبية",
            CityEn = "Riyadh",
            CityAr = "الرياض",
            ClinicType = ClinicType.Ambulatory,
            LicenseNumber = "LIC-001",
            LicenseExpiry = DateTime.Now.AddYears(2),
            IsActive = true,
            ComplianceScore = 0,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "admin@cbahi-portal.com"
        };

        await dbContext.Clinics.AddAsync(clinic);
        await dbContext.SaveChangesAsync();

        // Seed departments for ambulatory care
        var departmentCodes = new[]
        {
            (DepartmentCodeEnum.LD, "Leadership of the Organization", "قيادة المنظمة"),
            (DepartmentCodeEnum.PC, "Provision of Care", "تقديم الرعاية"),
            (DepartmentCodeEnum.LB, "Laboratory", "المختبر"),
            (DepartmentCodeEnum.RD, "Radiology Department", "قسم الأشعات"),
            (DepartmentCodeEnum.DN, "Dental", "الأسنان"),
            (DepartmentCodeEnum.MM, "Medication Management", "إدارة الأدوية"),
            (DepartmentCodeEnum.MOI, "Management of Information", "إدارة المعلومات"),
            (DepartmentCodeEnum.IPC, "Infection Prevention and Control", "الوقاية من العدوى والتحكم بها"),
            (DepartmentCodeEnum.FMS, "Facility Management and Safety", "إدارة المرافق والسلامة"),
            (DepartmentCodeEnum.DPU, "Dialysis Patient Unit", "وحدة مرضى غسيل الكلى"),
            (DepartmentCodeEnum.DA, "Dental Anesthesia", "تخدير الأسنان")
        };

        var departments = departmentCodes.Select(dc => new Department
        {
            NameEn = dc.Item2,
            NameAr = dc.Item3,
            DepartmentCode = dc.Item1,
            ClinicId = clinic.Id,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "admin@cbahi-portal.com"
        }).ToList();

        await dbContext.Departments.AddRangeAsync(departments);
        await dbContext.SaveChangesAsync();
    }
}
