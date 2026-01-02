namespace DonkeyWork.DeviceManager.Api.Services;

using DonkeyWork.DeviceManager.Common.Models;
using DonkeyWork.DeviceManager.Common.Models.Registration;

/// <summary>
/// Service for handling device registration completion.
/// </summary>
public interface IDeviceRegistrationService
{
    /// <summary>
    /// Looks up a device registration by three-word code and returns user's buildings and rooms.
    /// User ID and Tenant ID are derived from RequestContext.
    /// </summary>
    /// <param name="threeWordCode">The three-word registration code.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Registration lookup result with buildings and rooms.</returns>
    Task<DeviceRegistrationLookupResponse> LookupRegistrationAsync(string threeWordCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Completes device registration by creating a Keycloak device user and sending credentials via SignalR.
    /// User ID and Tenant ID are derived from RequestContext.
    /// </summary>
    /// <param name="threeWordCode">The three-word registration code.</param>
    /// <param name="roomId">The room ID where the device will be placed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The device credentials sent to the device.</returns>
    Task<DeviceCredentialsResponse> CompleteRegistrationAsync(string threeWordCode, Guid roomId, CancellationToken cancellationToken = default);
}
