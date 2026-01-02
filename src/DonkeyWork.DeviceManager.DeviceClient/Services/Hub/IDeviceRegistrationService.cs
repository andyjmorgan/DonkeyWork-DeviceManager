using DonkeyWork.DeviceManager.DeviceClient.Models;

namespace DonkeyWork.DeviceManager.DeviceClient.Services.Hub;

/// <summary>
/// Service for handling device registration flow via SignalR.
/// </summary>
public interface IDeviceRegistrationService
{
    /// <summary>
    /// Registers the device and waits for credentials.
    /// </summary>
    /// <param name="timeoutMinutes">Maximum time to wait for registration completion. Default is 5 minutes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Device tokens if registration succeeds; otherwise, null.</returns>
    Task<DeviceTokens?> RegisterDeviceAsync(int timeoutMinutes = 5, CancellationToken cancellationToken = default);
}
