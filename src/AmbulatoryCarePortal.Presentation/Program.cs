using AmbulatoryCarePortal.Application.BackgroundJobs;
using AmbulatoryCarePortal.Application.DependencyInjection;
using AmbulatoryCarePortal.Application.Settings;
using AmbulatoryCarePortal.Domain.Entities;
using AmbulatoryCarePortal.Infrastructure.Data;
using AmbulatoryCarePortal.Infrastructure.Data.Seed;
using AmbulatoryCarePortal.Infrastructure.DependencyInjection;
using AmbulatoryCarePortal.Presentation.DependencyInjection;
using AmbulatoryCarePortal.Presentation.Middleware;
using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using QuestPDF.Infrastructure;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", Serilog.Events.LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentName()
    .Enrich.WithCorrelationId()
    .WriteTo.Console()
    .WriteTo.File(
        "logs/app-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        fileSizeLimitBytes: 10485760)
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, lc) =>
    {
        lc.ReadFrom.Configuration(ctx.Configuration)
          .MinimumLevel.Information()
          .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
          .MinimumLevel.Override("Microsoft.Hosting.Lifetime", Serilog.Events.LogEventLevel.Information)
          .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
          .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)
          .Enrich.FromLogContext()
          .Enrich.WithMachineName()
          .Enrich.WithEnvironmentName()
          .Enrich.WithCorrelationId()
          .WriteTo.Console()
          .WriteTo.File(
              "logs/app-.log",
              rollingInterval: RollingInterval.Day,
              retainedFileCountLimit: 30,
              fileSizeLimitBytes: 10485760);

        var connStr = ctx.Configuration.GetConnectionString("DefaultConnection")
            ?? Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");
        if (!string.IsNullOrEmpty(connStr))
        {
            try
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
            catch
            {
                // MSSqlServer sink unavailable (expected during EF Core design-time
                // when the database doesn't exist yet). Console + File sinks remain active.
            }
        }
    });

    builder.Services.AddInfrastructureServices(builder.Configuration);
    builder.Services.AddApplicationServices();
    builder.Services.AddPresentationServices();
    builder.Services.AddIdentityConfiguration();
    builder.Services.AddCorsConfiguration();
    builder.Services.AddRateLimiting();
    builder.Services.AddObservability(builder.Configuration);
    builder.Services.AddRedisCache(builder.Configuration);
    builder.Services.AddHangfireJobs(builder.Configuration);

    builder.Services.AddOptions<DatabaseSettings>()
        .BindConfiguration(DatabaseSettings.SectionName)
        .ValidateDataAnnotations()
        .ValidateOnStart();
    builder.Services.AddOptions<NotificationSettings>()
        .BindConfiguration(NotificationSettings.SectionName)
        .ValidateDataAnnotations()
        .ValidateOnStart();
    builder.Services.AddOptions<FileUploadSettings>()
        .BindConfiguration(FileUploadSettings.SectionName)
        .ValidateDataAnnotations()
        .ValidateOnStart();
    builder.Services.AddOptions<RedisSettings>()
        .BindConfiguration(RedisSettings.SectionName)
        .ValidateDataAnnotations()
        .ValidateOnStart();
    builder.Services.AddOptions<SecuritySettings>()
        .BindConfiguration(SecuritySettings.SectionName)
        .ValidateDataAnnotations()
        .ValidateOnStart();
    builder.Services.AddOptions<EmailSettings>()
        .BindConfiguration(EmailSettings.SectionName)
        .ValidateDataAnnotations()
        .ValidateOnStart();

    builder.Services.AddRazorPages();
    builder.Services.AddControllersWithViews();

    QuestPDF.Settings.License = LicenseType.Community;

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        var env = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();

        if (env.IsDevelopment())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.Database.MigrateAsync();
        }

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        await RolePermissionsSeeder.SeedRolesWithPermissionsAsync(roleManager);

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var adminPassword = builder.Configuration.GetSection("Security")["AdminPassword"]
            ?? builder.Configuration["AdminPassword"]
            ?? Environment.GetEnvironmentVariable("ADMIN_PASSWORD")
            ?? throw new InvalidOperationException("AdminPassword not configured. Set Security:AdminPassword in appsettings, the AdminPassword key, or the ADMIN_PASSWORD environment variable.");
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
        Predicate = check => check.Tags.Contains("ready"),
        ResponseWriter = WriteHealthCheckResponseAsync
    });
    app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready"),
        ResponseWriter = WriteHealthCheckResponseAsync
    });
    app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("live"),
        ResponseWriter = WriteHealthCheckResponseAsync
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

    app.UseMiddleware<LogContextEnrichmentMiddleware>();
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
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

static async Task WriteHealthCheckResponseAsync(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json; charset=utf-8";

    var response = new
    {
        status = report.Status.ToString(),
        totalDuration = report.TotalDuration.TotalMilliseconds,
        entries = report.Entries.ToDictionary(
            e => e.Key,
            e => new
            {
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.TotalMilliseconds,
                data = e.Value.Data.Count > 0 ? e.Value.Data : null
            })
    };

    await System.Text.Json.JsonSerializer.SerializeAsync(context.Response.Body, response);
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
