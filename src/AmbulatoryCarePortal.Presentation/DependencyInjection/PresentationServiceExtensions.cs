using Microsoft.Extensions.DependencyInjection;
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

        return services;
    }
}
