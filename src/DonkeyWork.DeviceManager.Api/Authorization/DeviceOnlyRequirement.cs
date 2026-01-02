namespace DonkeyWork.DeviceManager.Api.Authorization;

using DonkeyWork.DeviceManager.Backend.Common.RequestContextServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

/// <summary>
/// Authorization requirement that ensures the connection is from a device (not a user).
/// </summary>
public class DeviceOnlyRequirement : IAuthorizationRequirement
{
}

/// <summary>
/// Authorization handler that checks if the connection is from a device.
/// </summary>
public class DeviceOnlyAuthorizationHandler : AuthorizationHandler<DeviceOnlyRequirement>
{
    private readonly IRequestContextProvider _requestContextProvider;
    private readonly ILogger<DeviceOnlyAuthorizationHandler> _logger;

    public DeviceOnlyAuthorizationHandler(
        IRequestContextProvider requestContextProvider,
        ILogger<DeviceOnlyAuthorizationHandler> logger)
    {
        _requestContextProvider = requestContextProvider;
        _logger = logger;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        DeviceOnlyRequirement requirement)
    {
        var isDeviceSession = _requestContextProvider.Context.IsDeviceSession;

        if (isDeviceSession)
        {
            _logger.LogDebug("Device-only authorization succeeded - DeviceUserId: {UserId}, TenantId: {TenantId}",
                _requestContextProvider.Context.UserId,
                _requestContextProvider.Context.TenantId);

            context.Succeed(requirement);
        }
        else
        {
            _logger.LogWarning("Device-only authorization failed - User connection attempted to access device-only resource. UserId: {UserId}",
                _requestContextProvider.Context.UserId);
        }

        return Task.CompletedTask;
    }
}
