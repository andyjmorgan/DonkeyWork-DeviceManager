namespace DonkeyWork.DeviceManager.Persistence.Services;

using DonkeyWork.DeviceManager.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class MigrationService(DeviceManagerContext deviceManagerContext, ILogger<MigrationService> logger) : IMigrationService
{
    public async Task MigrateAsync()
    {
        var pendingMigrations = await deviceManagerContext.Database.GetPendingMigrationsAsync();
        if (pendingMigrations.Any())
        {
            logger.LogInformation("Applying pending migrations:");
            await deviceManagerContext.Database.MigrateAsync();
        }
    }
}