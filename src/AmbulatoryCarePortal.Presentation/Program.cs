using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using AmbulatoryCarePortal.Application.BackgroundJobs;
using AmbulatoryCarePortal.Application.DependencyInjection;
using AmbulatoryCarePortal.Domain.Entities;
using AmbulatoryCarePortal.Infrastructure.Data;
using AmbulatoryCarePortal.Infrastructure.Data.Seed;
using AmbulatoryCarePortal.Infrastructure.DependencyInjection;
using AmbulatoryCarePortal.Presentation.DependencyInjection;
using AmbulatoryCarePortal.Presentation.Middleware;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/app-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, lc) =>
    {
        lc.MinimumLevel.Information()
          .WriteTo.Console()
          .WriteTo.File("logs/app-.log", rollingInterval: RollingInterval.Day);

        var connStr = ctx.Configuration.GetConnectionString("DefaultConnection")
            ?? Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");
        if (!string.IsNullOrEmpty(connStr))
        {
            lc.WriteTo.MSSqlServer(
                connectionString: connStr,
                sinkOptions: new Serilog.Sinks.MSSqlServer.MSSqlServerSinkOptions
                {
                    TableName = "LogEvents",
                    AutoCreateSqlTable = true,
                    BatchPostingLimit = 50
                });
        }
    });

    builder.Services.AddInfrastructureServices(builder.Configuration);
    builder.Services.AddApplicationServices();
    builder.Services.AddPresentationServices();
    builder.Services.AddIdentityConfiguration();
    builder.Services.AddCorsConfiguration();
    builder.Services.AddRateLimiting();
    builder.Services.AddObservability();
    builder.Services.AddRedisCache(builder.Configuration);
    builder.Services.AddHangfireJobs(builder.Configuration);

    builder.Services.AddRazorPages();
    builder.Services.AddControllersWithViews();

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        await RolePermissionsSeeder.SeedRolesWithPermissionsAsync(roleManager);

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var adminPassword = builder.Configuration["AdminPassword"]
            ?? throw new InvalidOperationException("AdminPassword environment variable not set.");
        await SeedAdminUserAsync(userManager, adminPassword);

        var dbCtx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await DepartmentSeeder.SeedDepartmentsAsync(dbCtx, "admin@cbahi-portal.com");
        await dbCtx.SaveChangesAsync();

        var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
        HangfireConfiguration.RegisterRecurringJobs(recurringJobManager);
    }

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
    }
    else
    {
        app.UseDeveloperExceptionPage();
    }

    app.UseMiddleware<ExceptionMiddleware>();
    app.UseMiddleware<RequestLoggingMiddleware>();
    app.UseMiddleware<AuditMiddleware>();

    app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = _ => false,
        ResponseWriter = async (ctx, r) =>
        {
            await ctx.Response.WriteAsync("Healthy");
        }
    });
    app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready")
    });
    app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = _ => true
    });

    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization =
        [
            new HangfireDashboardAuthorizationFilter()
        ],
        StatsPollingInterval = 5000,
        DashboardTitle = "CBAHI Portal - Job Dashboard"
    });

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();
    app.UseRateLimiter();
    app.UseSession();

    app.UseAuthentication();
    app.UseAuthorization();

    app.UseMiddleware<SecurityHeadersMiddleware>();
    app.UseMiddleware<ClinicAccessMiddleware>();

    app.MapControllerRoute(
        name: "areas",
        pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}"
    );

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}"
    );

    app.MapRazorPages();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

static async Task SeedAdminUserAsync(UserManager<AppUser> userManager, string adminPassword)
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
            IsActive = true
        };

        var result = await userManager.CreateAsync(adminUser, adminPassword);

        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "SuperAdmin");
        }
    }
}
