namespace DonkeyWork.DeviceManager.Api.SignalR;

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

/// <summary>
/// Custom SignalR user ID provider that uses the 'sub' claim from JWT.
/// This allows targeting specific devices/users by their Keycloak user ID.
/// </summary>
public class RequestContextUserIdProvider : IUserIdProvider
{
    private readonly ILogger<RequestContextUserIdProvider> _logger;

    public RequestContextUserIdProvider(ILogger<RequestContextUserIdProvider> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Gets the user identifier from the connection's ClaimsPrincipal.
    /// Uses the 'sub' (subject) claim which contains the Keycloak user ID.
    /// </summary>
    public string? GetUserId(HubConnectionContext connection)
    {
        // Use the 'sub' claim which is the Keycloak user ID
        // This works for both device tokens and user tokens
        var userId = connection.User?.FindFirst("sub")?.Value;

        _logger.LogDebug(
            "SignalR UserIdProvider mapping ConnectionId {ConnectionId} to UserId {UserId}",
            connection.ConnectionId, userId);

        return userId;
    }
}
