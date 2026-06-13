using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using AmbulatoryCarePortal.Presentation.Authorization;
using AmbulatoryCarePortal.Presentation.Filters;
using AmbulatoryCarePortal.Presentation.Helpers;

namespace AmbulatoryCarePortal.Presentation.DependencyInjection;

public static class PresentationServiceExtensions
{
    public static IServiceCollection AddPresentationServices(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ClinicAuthorizationFilter>();
        services.AddScoped<ITranslationService, TranslationService>();

        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

        return services;
    }

    public static IServiceCollection AddCorsConfiguration(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("Default", policy =>
            {
                policy.WithOrigins()
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .SetIsOriginAllowed(origin => new Uri(origin).Host == "localhost")
                      .Build();
            });
        });

        return services;
    }

    public static IServiceCollection AddRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddFixedWindowLimiter("Login", cfg =>
            {
                cfg.PermitLimit = 5;
                cfg.Window = TimeSpan.FromMinutes(1);
                cfg.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                cfg.QueueLimit = 0;
            });

            options.AddFixedWindowLimiter("Api", cfg =>
            {
                cfg.PermitLimit = 100;
                cfg.Window = TimeSpan.FromMinutes(1);
                cfg.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                cfg.QueueLimit = 10;
            });

            options.AddFixedWindowLimiter("Global", cfg =>
            {
                cfg.PermitLimit = 1000;
                cfg.Window = TimeSpan.FromMinutes(1);
                cfg.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                cfg.QueueLimit = 50;
            });
        });

        return services;
    }
}
