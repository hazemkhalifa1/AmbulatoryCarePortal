using Microsoft.Extensions.DependencyInjection;
using AmbulatoryCarePortal.Presentation.Filters;

namespace AmbulatoryCarePortal.Presentation.DependencyInjection;

public static class PresentationServiceExtensions
{
    public static IServiceCollection AddPresentationServices(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ClinicAuthorizationFilter>();

        return services;
    }
}
