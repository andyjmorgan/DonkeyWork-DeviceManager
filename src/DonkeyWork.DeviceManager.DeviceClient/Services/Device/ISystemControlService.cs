namespace DonkeyWork.DeviceManager.DeviceClient.Services.Device;

/// <summary>
/// Service for system-level control operations (restart, shutdown).
/// </summary>
public interface ISystemControlService
{
    /// <summary>
    /// Restarts the system.
    /// </summary>
    Task RestartAsync();

    /// <summary>
    /// Shuts down the system.
    /// </summary>
    Task ShutdownAsync();
}
