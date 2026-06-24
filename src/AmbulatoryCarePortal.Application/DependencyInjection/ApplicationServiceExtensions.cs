using Microsoft.Extensions.DependencyInjection;
using FluentValidation;
using AmbulatoryCarePortal.Application.Interfaces;
using AmbulatoryCarePortal.Application.Mappings;
using AmbulatoryCarePortal.Application.Services;
using AmbulatoryCarePortal.Application.Validators;

namespace AmbulatoryCarePortal.Application.DependencyInjection;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // AutoMapper
        services.AddAutoMapper(cfg =>
        {
            cfg.AddProfile<MappingProfile>();
        });

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
        services.AddScoped<MailKitEmailSender>();
        services.AddScoped<IEmailService, HangfireEmailService>();
        services.AddScoped<IBackgroundJobService, BackgroundJobService>();
        services.AddScoped<IAdvancedNotificationService, AdvancedNotificationService>();
        services.AddScoped<IAdvancedSearchService, AdvancedSearchService>();
        services.AddScoped<IBulkOperationsService, BulkOperationsService>();
        services.AddScoped<IDataExportService, DataExportService>();
        services.AddScoped<IFormService, FormService>();

        // Document services
        services.AddScoped<IDocumentTemplateService, DocumentTemplateService>();
        services.AddScoped<IClinicDocumentService, ClinicDocumentService>();
        services.AddScoped<ITemplateVariableService, TemplateVariableService>();
        services.AddScoped<IClinicTemplateAssignmentService, ClinicTemplateAssignmentService>();
        services.AddScoped<IDocumentGenerationService, DocumentGenerationService>();

        // Compliance Score Engine
        services.AddScoped<IComplianceScoreService, ComplianceScoreService>();

        // Signature Management
        services.AddScoped<IClinicSignatureService, ClinicSignatureService>();
        services.AddScoped<ITemplateSignerService, TemplateSignerService>();

        // Settings
        services.AddScoped<ISettingsService, SettingsService>();

        // Compliance
        services.AddScoped<IComplianceCalendarService, ComplianceCalendarService>();

        return services;
    }
}
