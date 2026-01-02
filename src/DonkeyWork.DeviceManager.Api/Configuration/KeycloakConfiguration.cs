namespace DonkeyWork.DeviceManager.Api.Configuration;

using System.ComponentModel.DataAnnotations;

public record KeycloakConfiguration
{
    /// <summary>
    /// Gets the keycloak authority (public URL used for JWT issuer validation).
    /// </summary>
    [Required]
    public required string Authority { get; init; }

    /// <summary>
    /// Gets the internal keycloak authority (optional, used for backchannel HTTP calls).
    /// Use this to avoid hairpin NAT issues when running in k8s.
    /// Example: http://keycloak.infrastructure.svc.cluster.local:8080/realms/DeviceManager
    /// If not set, Authority will be used for all communications.
    /// </summary>
    public string? InternalAuthority { get; init; }

    /// <summary>
    /// Gets the keycloak client id.
    /// </summary>
    [Required]
    public required string ClientId { get; init; }

    /// <summary>
    /// Gets the keycloak client secret.
    /// </summary>
    [Required]
    public required string ClientSecret { get; init; }

    /// <summary>
    /// Gets the redirect URI for OAuth callbacks.
    /// </summary>
    [Required]
    public required string RedirectUri { get; init; }

    /// <summary>
    /// Gets the admin client id for Admin API access.
    /// </summary>
    [Required]
    public required string AdminClientId { get; init; }

    /// <summary>
    /// Gets the admin client secret for Admin API access.
    /// </summary>
    [Required]
    public required string AdminClientSecret { get; init; }

    /// <summary>
    /// Gets the authority to use for backchannel HTTP calls.
    /// Returns InternalAuthority if set (to avoid hairpin NAT in k8s), otherwise returns Authority.
    /// </summary>
    public string BackchannelAuthority => InternalAuthority ?? Authority;
}