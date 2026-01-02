namespace DonkeyWork.DeviceManager.Common.Models.Device;

/// <summary>
/// Response containing refreshed device tokens.
/// </summary>
public record RefreshTokenResponse
{
    /// <summary>
    /// Gets the new access token.
    /// </summary>
    public required string AccessToken { get; init; }

    /// <summary>
    /// Gets the new refresh token (may be the same as the old one).
    /// </summary>
    public required string RefreshToken { get; init; }

    /// <summary>
    /// Gets the number of seconds until the access token expires.
    /// </summary>
    public required int ExpiresIn { get; init; }
}
