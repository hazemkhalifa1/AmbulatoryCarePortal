using Microsoft.AspNetCore.Identity;
using Serilog;
using AmbulatoryCarePortal.Application.DependencyInjection;
using AmbulatoryCarePortal.Domain.Entities;
using AmbulatoryCarePortal.Infrastructure.Data;
using AmbulatoryCarePortal.Infrastructure.Data.Seed;
using AmbulatoryCarePortal.Infrastructure.DependencyInjection;
using AmbulatoryCarePortal.Presentation.DependencyInjection;
using AmbulatoryCarePortal.Presentation.Helpers;
using AmbulatoryCarePortal.Presentation.Middleware;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/app-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();


try
{
    var builder = WebApplication.CreateBuilder(args);

    // Add Serilog
    builder.Host.UseSerilog();

    // Add services
    builder.Services.AddInfrastructureServices(builder.Configuration);
    builder.Services.AddApplicationServices();
    builder.Services.AddPresentationServices();

    // Add Identity
    builder.Services
        .AddIdentity<AppUser, IdentityRole>(options =>
        {
            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

    // Add custom claims factory to inject ClinicId claim
    builder.Services.AddScoped<IUserClaimsPrincipalFactory<AppUser>, ClinicClaimsPrincipalFactory>();

    // Add Razor Pages
    builder.Services.AddRazorPages();

    // Add MVC
    builder.Services.AddControllersWithViews();

    // Add session
    builder.Services.AddSession(options =>
    {
        options.IdleTimeout = TimeSpan.FromMinutes(20);
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
    });

    var app = builder.Build();

    // Initialize database
    await DbInitializer.InitializeAsync(app.Services);

    // Configure the HTTP request pipeline
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
    }
    else
    {
        app.UseDeveloperExceptionPage();
    }

    // Add custom middleware
    app.UseMiddleware<ExceptionMiddleware>();
    app.UseMiddleware<AuditMiddleware>();

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();
    app.UseSession();

    // Authentication and Authorization
    app.UseAuthentication();
    app.UseAuthorization();

    // Clinic access check (after auth)
    app.UseMiddleware<ClinicAccessMiddleware>();

    // Endpoints
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
