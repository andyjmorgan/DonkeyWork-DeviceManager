namespace DonkeyWork.DeviceManager.Api.Services;

public interface ITenantProvisioningService
{
    /// <summary>
    /// Provisions a new tenant and initial user asynchronously.
    /// </summary>
    /// <param name="tenantId">The unique identifier for the tenant.</param>
    /// <param name="keycloakUserId">The Keycloak user ID who owns the tenant.</param>
    /// <param name="email">The user's email address.</param>
    /// <param name="fullName">The user's full name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ProvisionTenantAsync(Guid tenantId, string keycloakUserId, string email, string fullName, CancellationToken cancellationToken = default);
}
