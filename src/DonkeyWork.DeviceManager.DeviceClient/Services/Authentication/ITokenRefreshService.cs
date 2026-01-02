using DonkeyWork.DeviceManager.DeviceClient.Models;

namespace DonkeyWork.DeviceManager.DeviceClient.Services.Authentication;

/// <summary>
/// Service for refreshing device authentication tokens with Keycloak.
/// </summary>
public interface ITokenRefreshService
{
    /// <summary>
    /// Refreshes the access token using the refresh token.
    /// </summary>
    /// <param name="currentTokens">The current device tokens containing the refresh token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated device tokens with new access token; or null if refresh fails.</returns>
    Task<DeviceTokens?> RefreshAccessTokenAsync(DeviceTokens currentTokens, CancellationToken cancellationToken = default);
}
