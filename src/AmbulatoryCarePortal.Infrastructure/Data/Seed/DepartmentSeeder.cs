using Microsoft.EntityFrameworkCore;
using AmbulatoryCarePortal.Domain.Entities;
using AmbulatoryCarePortal.Domain.Enums;

namespace AmbulatoryCarePortal.Infrastructure.Data.Seed;

public static class DepartmentSeeder
{
    private static readonly (DepartmentCodeEnum Code, string NameEn, string NameAr)[] CbahiDepartments =
    {
        (DepartmentCodeEnum.LD,  "Leadership of the Organization",     "قيادة المنظمة"),
        (DepartmentCodeEnum.PC,  "Provision of Care",                 "تقديم الرعاية"),
        (DepartmentCodeEnum.LB,  "Laboratory",                        "المختبر"),
        (DepartmentCodeEnum.RD,  "Radiology Department",              "قسم الأشعات"),
        (DepartmentCodeEnum.DN,  "Dental",                            "الأسنان"),
        (DepartmentCodeEnum.MM,  "Medication Management",             "إدارة الأدوية"),
        (DepartmentCodeEnum.MOI, "Management of Information",         "إدارة المعلومات"),
        (DepartmentCodeEnum.IPC, "Infection Prevention and Control",  "الوقاية من العدوى والتحكم بها"),
        (DepartmentCodeEnum.FMS, "Facility Management and Safety",    "إدارة المرافق والسلامة"),
        (DepartmentCodeEnum.DPU, "Dialysis Patient Unit",             "وحدة مرضى غسيل الكلى"),
        (DepartmentCodeEnum.DA,  "Dental Anesthesia",                 "تخدير الأسنان"),
        (DepartmentCodeEnum.DL,  "Dental Laboratory",                 "مختبر الأسنان")
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
                ClinicType = ClinicType.Ambulatory,
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

        var existingDepartmentCodes = (await dbContext.Departments
            .Where(d => d.ClinicId == clinic.Id)
            .Select(d => d.DepartmentCode)
            .ToListAsync()).ToHashSet();

        var newDepartments = new List<Department>();

        foreach (var (code, nameEn, nameAr) in CbahiDepartments)
        {
            if (existingDepartmentCodes.Contains(code))
                continue;

            newDepartments.Add(new Department
            {
                NameEn = nameEn,
                NameAr = nameAr,
                DepartmentCode = code,
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
