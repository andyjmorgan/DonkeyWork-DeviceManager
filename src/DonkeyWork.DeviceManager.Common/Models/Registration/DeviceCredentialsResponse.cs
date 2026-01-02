namespace DonkeyWork.DeviceManager.Common.Models.Registration;

/// <summary>
/// Device credentials sent via SignalR after registration completes.
/// </summary>
public record DeviceCredentialsResponse
{
    /// <summary>
    /// Gets the JWT access token for the device.
    /// </summary>
    public required string AccessToken { get; init; }

    /// <summary>
    /// Gets the refresh token for the device.
    /// </summary>
    public required string RefreshToken { get; init; }

    /// <summary>
    /// Gets the device's Keycloak user ID.
    /// </summary>
    public required Guid DeviceUserId { get; init; }

    /// <summary>
    /// Gets the tenant ID the device belongs to.
    /// </summary>
    public required Guid TenantId { get; init; }
}
