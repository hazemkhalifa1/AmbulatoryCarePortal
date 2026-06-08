using Microsoft.Extensions.DependencyInjection;
using FluentValidation;
using AmbulatoryCarePortal.Application.Interfaces;
using AmbulatoryCarePortal.Application.Mappings;
using AmbulatoryCarePortal.Application.Services;

namespace AmbulatoryCarePortal.Application.DependencyInjection;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // AutoMapper
        services.AddAutoMapper(typeof(MappingProfile));

        // FluentValidation
        services.AddValidatorsFromAssemblyContaining(typeof(ApplicationServiceExtensions));

        // Core services
        services.AddScoped<IClinicService, ClinicService>();
        services.AddScoped<IPolicyDocumentService, PolicyDocumentService>();
        services.AddScoped<IKPIService, KPIService>();
        services.AddScoped<IChecklistService, ChecklistService>();
        services.AddScoped<IHrService, HrService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<INotificationService, NotificationService>();

        // Additional services
        services.AddScoped<IReportingService, ReportingService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IBackgroundJobService, BackgroundJobService>();
        services.AddScoped<IAdvancedNotificationService, AdvancedNotificationService>();
        services.AddScoped<IAdvancedSearchService, AdvancedSearchService>();
        services.AddScoped<IBulkOperationsService, BulkOperationsService>();
        services.AddScoped<IDataExportService, DataExportService>();
        services.AddScoped<IFormService, FormService>();

        // Background services
        services.AddHostedService<NotificationBackgroundService>();

        return services;
    }
}
