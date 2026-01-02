namespace DonkeyWork.DeviceManager.Api.Filters;

using DonkeyWork.DeviceManager.Backend.Common.RequestContextServices;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

/// <summary>
/// SignalR hub filter that populates the RequestContext from JWT claims
/// for each hub method invocation.
/// </summary>
public class RequestContextHubFilter : IHubFilter
{
    private readonly ILogger<RequestContextHubFilter> _logger;

    public RequestContextHubFilter(ILogger<RequestContextHubFilter> logger)
    {
        _logger = logger;
    }

    public async ValueTask<object?> InvokeMethodAsync(
        HubInvocationContext invocationContext,
        Func<HubInvocationContext, ValueTask<object?>> next)
    {
        // Get RequestContextProvider from the hub context
        var requestContextProvider = invocationContext.ServiceProvider
            .GetService(typeof(IRequestContextProvider)) as IRequestContextProvider;

        if (requestContextProvider != null)
        {
            var success = requestContextProvider.Context.PopulateFromPrincipal(invocationContext.Context.User, _logger);

            if (success)
            {
                _logger.LogDebug("SignalR Hub Method '{Method}' - UserId: {UserId}, TenantId: {TenantId}, IsDevice: {IsDevice}, RequestId: {RequestId}",
                    invocationContext.HubMethodName,
                    requestContextProvider.Context.UserId,
                    requestContextProvider.Context.TenantId,
                    requestContextProvider.Context.IsDeviceSession,
                    requestContextProvider.Context.RequestId);
            }
            else
            {
                _logger.LogWarning("SignalR Hub - Failed to populate RequestContext for method '{Method}'",
                    invocationContext.HubMethodName);
            }
        }
        else
        {
            _logger.LogWarning("SignalR Hub - RequestContextProvider not available for method '{Method}'",
                invocationContext.HubMethodName);
        }

        // Continue with the hub method invocation
        return await next(invocationContext);
    }

    public Task OnConnectedAsync(HubLifetimeContext context, Func<HubLifetimeContext, Task> next)
    {
        // Populate RequestContext on connection
        var requestContextProvider = context.ServiceProvider
            .GetService(typeof(IRequestContextProvider)) as IRequestContextProvider;

        if (requestContextProvider != null)
        {
            var success = requestContextProvider.Context.PopulateFromPrincipal(context.Context.User, _logger);

            if (success)
            {
                _logger.LogInformation("SignalR Connection - UserId: {UserId}, TenantId: {TenantId}, IsDevice: {IsDevice}, ConnectionId: {ConnectionId}",
                    requestContextProvider.Context.UserId,
                    requestContextProvider.Context.TenantId,
                    requestContextProvider.Context.IsDeviceSession,
                    context.Context.ConnectionId);
            }
            else
            {
                _logger.LogWarning("SignalR Connection - Failed to populate RequestContext for ConnectionId: {ConnectionId}",
                    context.Context.ConnectionId);
            }
        }

        return next(context);
    }

    public Task OnDisconnectedAsync(HubLifetimeContext context, Exception? exception, Func<HubLifetimeContext, Exception?, Task> next)
    {
        return next(context, exception);
    }
}
