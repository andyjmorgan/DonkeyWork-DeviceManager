namespace DonkeyWork.DeviceManager.Common.Models.Device;

/// <summary>
/// Request to refresh device access token.
/// </summary>
public record RefreshTokenRequest
{
    /// <summary>
    /// Gets the device's refresh token.
    /// </summary>
    public required string RefreshToken { get; init; }
}
