using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AmbulatoryCarePortal.Application.Interfaces;
using AmbulatoryCarePortal.Application.Interfaces.Repositories;
using AmbulatoryCarePortal.Infrastructure.Data;
using AmbulatoryCarePortal.Infrastructure.Repositories;
using AmbulatoryCarePortal.Infrastructure.Services;
using AmbulatoryCarePortal.Infrastructure.UnitOfWork;

namespace AmbulatoryCarePortal.Infrastructure.DependencyInjection;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // DbContext with audit interceptor
        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found. Set appsettings.json or DB_CONNECTION_STRING environment variable.");

            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.CommandTimeout(120);
                sqlOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                sqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null);
            });

            options.AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>());
        });

        // UnitOfWork - registered against Application interface
        services.AddScoped<IUnitOfWork, AmbulatoryCarePortal.Infrastructure.UnitOfWork.UnitOfWork>();

        // Generic Repository - registered against Application interface
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

        // Encryption
        services.AddScoped<IEncryptionService, DataProtectionEncryptionService>();

        // Distributed cache service
        services.AddScoped<ICacheService, CacheService>();

        // Audit interceptor
        services.AddScoped<AuditSaveChangesInterceptor>();

        return services;
    }
}
