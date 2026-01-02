namespace DonkeyWork.DeviceManager.DeviceClient.Models;

/// <summary>
/// Represents the device authentication tokens stored locally.
/// </summary>
public record DeviceTokens
{
    /// <summary>
    /// Gets or sets the JWT access token.
    /// </summary>
    public required string AccessToken { get; set; }

    /// <summary>
    /// Gets or sets the refresh token for obtaining new access tokens.
    /// </summary>
    public required string RefreshToken { get; set; }

    /// <summary>
    /// Gets or sets the device's Keycloak user ID.
    /// </summary>
    public required Guid DeviceUserId { get; set; }

    /// <summary>
    /// Gets or sets the tenant ID the device belongs to.
    /// </summary>
    public required Guid TenantId { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the access token expires.
    /// </summary>
    public required DateTime ExpiresAtUtc { get; set; }

    /// <summary>
    /// Checks if the access token has expired or will expire within the specified buffer time.
    /// </summary>
    /// <param name="bufferMinutes">Buffer time in minutes before actual expiry. Default is 5 minutes.</param>
    /// <returns>True if the token has expired or will expire soon; otherwise, false.</returns>
    public bool IsExpiredOrExpiringSoon(int bufferMinutes = 5)
    {
        return DateTime.UtcNow >= ExpiresAtUtc.AddMinutes(-bufferMinutes);
    }
}
