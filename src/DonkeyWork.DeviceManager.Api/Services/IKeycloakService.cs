namespace DonkeyWork.DeviceManager.Api.Services;

/// <summary>
/// Service for interacting with Keycloak admin APIs.
/// Centralizes all Keycloak operations to avoid code duplication.
/// </summary>
public interface IKeycloakService
{
    /// <summary>
    /// Gets an admin access token for service-to-service authentication with Keycloak.
    /// </summary>
    Task<string> GetAdminAccessTokenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new device user in Keycloak.
    /// </summary>
    /// <param name="username">The username for the device user.</param>
    /// <param name="email">The email for the device user.</param>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The Keycloak user ID assigned to the new user.</returns>
    Task<string> CreateDeviceUserAsync(string username, string email, Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the password for a device user in Keycloak.
    /// </summary>
    /// <param name="keycloakUserId">The Keycloak user ID.</param>
    /// <param name="password">The password to set.</param>
    /// <param name="temporary">Whether the password is temporary.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetUserPasswordAsync(string keycloakUserId, string password, bool temporary = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all required actions for a user in Keycloak.
    /// </summary>
    /// <param name="keycloakUserId">The Keycloak user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ClearRequiredActionsAsync(string keycloakUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets OAuth2 tokens for a device user using password grant.
    /// </summary>
    /// <param name="username">The username.</param>
    /// <param name="password">The password.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Tuple of (access_token, refresh_token, expires_in).</returns>
    Task<(string AccessToken, string RefreshToken, int ExpiresIn)> GetUserTokensAsync(string username, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a user's password by attempting to get tokens.
    /// </summary>
    /// <param name="username">The username.</param>
    /// <param name="password">The password.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if password is valid, false otherwise.</returns>
    Task<bool> ValidateUserPasswordAsync(string username, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a user from Keycloak.
    /// </summary>
    /// <param name="keycloakUserId">The Keycloak user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteUserAsync(string keycloakUserId, CancellationToken cancellationToken = default);
}
