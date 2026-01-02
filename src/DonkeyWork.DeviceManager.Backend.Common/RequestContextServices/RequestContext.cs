namespace DonkeyWork.DeviceManager.Backend.Common.RequestContextServices;

using System.Security.Claims;
using Microsoft.Extensions.Logging;

public class RequestContext
{
    /// <summary>
    /// Gets or sets the user Id.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the tenant Id.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Gets or sets the request Id.
    /// </summary>
    public Guid RequestId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the session is a device session.
    /// </summary>
    public bool IsDeviceSession { get; set; }

    /// <summary>
    /// Populates the RequestContext from JWT claims in a ClaimsPrincipal.
    /// </summary>
    /// <param name="principal">The ClaimsPrincipal containing JWT claims</param>
    /// <param name="logger">Optional logger for diagnostics</param>
    /// <returns>True if context was successfully populated, false otherwise</returns>
    public bool PopulateFromPrincipal(ClaimsPrincipal? principal, ILogger? logger = null)
    {
        if (principal == null)
        {
            logger?.LogWarning("Cannot populate RequestContext: principal is null");
            return false;
        }

        // Generate new request ID
        RequestId = Guid.NewGuid();

        // Extract sub claim (Keycloak user ID)
        var subClaim = principal.FindFirst("sub")?.Value;
        if (!string.IsNullOrEmpty(subClaim) && Guid.TryParse(subClaim, out var userId))
        {
            UserId = userId;
            logger?.LogDebug("Extracted UserId from 'sub' claim: {UserId}", userId);
        }
        else
        {
            logger?.LogWarning("Could not extract UserId from 'sub' claim. Value: {SubClaim}", subClaim);
        }

        // Extract tenantId claim
        var tenantIdClaim = principal.FindFirst("tenantId")?.Value;
        if (!string.IsNullOrEmpty(tenantIdClaim) && Guid.TryParse(tenantIdClaim, out var tenantId))
        {
            TenantId = tenantId;
            logger?.LogDebug("Extracted TenantId from 'tenantId' claim: {TenantId}", tenantId);
        }
        else
        {
            logger?.LogWarning("Could not extract TenantId from 'tenantId' claim. Value: {TenantIdClaim}", tenantIdClaim);
        }

        // Check if this is a device session
        var isDeviceClaim = principal.FindFirst("isDevice")?.Value;
        IsDeviceSession = isDeviceClaim == "true";

        logger?.LogDebug("RequestContext populated - UserId: {UserId}, TenantId: {TenantId}, IsDevice: {IsDevice}, RequestId: {RequestId}",
            UserId, TenantId, IsDeviceSession, RequestId);

        return UserId != Guid.Empty && TenantId != Guid.Empty;
    }
}