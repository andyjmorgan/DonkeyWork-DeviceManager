namespace DonkeyWork.DeviceManager.Api.Authorization;

using DonkeyWork.DeviceManager.Backend.Common.RequestContextServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

/// <summary>
/// Authorization requirement that ensures the connection is from a user (not a device).
/// </summary>
public class UserOnlyRequirement : IAuthorizationRequirement
{
}

/// <summary>
/// Authorization handler that checks if the connection is from a user.
/// </summary>
public class UserOnlyAuthorizationHandler : AuthorizationHandler<UserOnlyRequirement>
{
    private readonly IRequestContextProvider _requestContextProvider;
    private readonly ILogger<UserOnlyAuthorizationHandler> _logger;

    public UserOnlyAuthorizationHandler(
        IRequestContextProvider requestContextProvider,
        ILogger<UserOnlyAuthorizationHandler> logger)
    {
        _requestContextProvider = requestContextProvider;
        _logger = logger;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        UserOnlyRequirement requirement)
    {
        var isDeviceSession = _requestContextProvider.Context.IsDeviceSession;

        if (!isDeviceSession)
        {
            _logger.LogDebug("User-only authorization succeeded - UserId: {UserId}, TenantId: {TenantId}",
                _requestContextProvider.Context.UserId,
                _requestContextProvider.Context.TenantId);

            context.Succeed(requirement);
        }
        else
        {
            _logger.LogWarning("User-only authorization failed - Device connection attempted to access user-only resource. DeviceUserId: {UserId}",
                _requestContextProvider.Context.UserId);
        }

        return Task.CompletedTask;
    }
}
