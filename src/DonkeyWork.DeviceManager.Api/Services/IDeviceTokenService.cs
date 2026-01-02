namespace DonkeyWork.DeviceManager.Api.Services;

/// <summary>
/// Service for managing device authentication tokens.
/// </summary>
public interface IDeviceTokenService
{
    /// <summary>
    /// Refreshes a device's access token using its refresh token.
    /// </summary>
    /// <param name="refreshToken">The device's refresh token.</param>
    /// <returns>New access token, refresh token, and expiration info.</returns>
    Task<(string AccessToken, string RefreshToken, int ExpiresIn)?> RefreshDeviceTokenAsync(string refreshToken);
}
