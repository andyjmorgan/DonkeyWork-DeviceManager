namespace DonkeyWork.DeviceManager.Persistence.Services;

/// <summary>
/// A service for handling database migrations.
/// </summary>
public interface IMigrationService
{
    /// <summary>
    /// Performs database migration asynchronously.
    /// </summary>
    /// <returns>A Task.</returns>
    public Task MigrateAsync();
}