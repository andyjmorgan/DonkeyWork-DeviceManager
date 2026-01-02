namespace DonkeyWork.DeviceManager.Persistence.Extensions;

using DonkeyWork.DeviceManager.Persistence.Context;
using DonkeyWork.DeviceManager.Persistence.Interceptors;
using DonkeyWork.DeviceManager.Persistence.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPersistenceServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register interceptors
        services.AddScoped<CreatedOrUpdatedInterceptor>();
        services.AddScoped<IMigrationService, MigrationService>();

        // Add DbContext with interceptors
        services.AddDbContext<DeviceManagerContext>(
            (serviceProvider, options) =>
            {
                options.EnableDetailedErrors();
                options.EnableSensitiveDataLogging();
                options.UseNpgsql(
                    configuration.GetConnectionString(
                        nameof(DeviceManagerContext)),
                    o =>
                    {
                        o.MigrationsHistoryTable("__EFMigrationsHistory", "DeviceManager");
                        o.ConfigureDataSource(d => d.EnableDynamicJson());
                    });

                options.AddInterceptors(
                    serviceProvider.GetRequiredService<CreatedOrUpdatedInterceptor>());
            });

        return services;
    }
}