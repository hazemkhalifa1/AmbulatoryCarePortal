using Microsoft.EntityFrameworkCore;
using AmbulatoryCarePortal.Domain.Entities;
using AmbulatoryCarePortal.Domain.Enums;

namespace AmbulatoryCarePortal.Infrastructure.Data.Seed;

public static class DepartmentSeeder
{
    private static readonly (string Code, string NameEn, string NameAr)[] CbahiDepartments =
    {
        ("LD",  "Leadership of the Organization",     "قيادة المنظمة"),
        ("PC",  "Provision of Care",                 "تقديم الرعاية"),
        ("LB",  "Laboratory",                        "المختبر"),
        ("RD",  "Radiology Department",              "قسم الأشعات"),
        ("DN",  "Dental",                            "الأسنان"),
        ("MM",  "Medication Management",             "إدارة الأدوية"),
        ("MOI", "Management of Information",         "إدارة المعلومات"),
        ("IPC", "Infection Prevention and Control",  "الوقاية من العدوى والتحكم بها"),
        ("FMS", "Facility Management and Safety",    "إدارة المرافق والسلامة"),
        ("DPU", "Dialysis Patient Unit",             "وحدة مرضى غسيل الكلى"),
        ("DA",  "Dental Anesthesia",                 "تخدير الأسنان"),
        ("DL",  "Dental Laboratory",                 "مختبر الأسنان")
    };

    public static async Task SeedDepartmentsAsync(AppDbContext dbContext, string createdBy = "system")
    {
        var clinic = await dbContext.Clinics.FirstOrDefaultAsync();
        if (clinic == null)
        {
            clinic = new Clinic
            {
                Name = "Demo Clinic",
                NameAr = "العيادة التجريبية",
                CityEn = "Riyadh",
                CityAr = "الرياض",
                ClinicType = ClinicType.AMB,
                LicenseNumber = "LIC-001",
                LicenseExpiry = DateTime.Now.AddYears(2),
                IsActive = true,
                ComplianceScore = 0,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy
            };

            await dbContext.Clinics.AddAsync(clinic);
            await dbContext.SaveChangesAsync();
        }

        var existingCodes = (await dbContext.Departments
            .Where(d => d.ClinicId == clinic.Id)
            .Select(d => d.Code)
            .ToListAsync()).ToHashSet();

        var newDepartments = new List<Department>();

        foreach (var (code, nameEn, nameAr) in CbahiDepartments)
        {
            if (existingCodes.Contains(code))
                continue;

            newDepartments.Add(new Department
            {
                NameEn = nameEn,
                NameAr = nameAr,
                Code = code,
                ClinicId = clinic.Id,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy
            });
        }

        if (newDepartments.Count > 0)
        {
            await dbContext.Departments.AddRangeAsync(newDepartments);
            await dbContext.SaveChangesAsync();
        }
    }
}
