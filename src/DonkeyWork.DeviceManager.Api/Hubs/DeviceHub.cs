namespace DonkeyWork.DeviceManager.Api.Hubs;

using DonkeyWork.DeviceManager.Api.Authorization;
using DonkeyWork.DeviceManager.Api.Services.HubActivity;
using DonkeyWork.DeviceManager.Backend.Common.RequestContextServices;
using DonkeyWork.DeviceManager.Common.SignalR;
using DonkeyWork.DeviceManager.Persistence.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

/// <summary>
/// SignalR hub for authenticated device communication.
/// Devices must connect with a valid JWT token.
/// Only devices (not users) can connect to this hub.
/// </summary>
[Authorize(Policy = AuthorizationPolicies.DeviceOnly)]
public class DeviceHub : Hub
{
    private readonly DeviceManagerContext _dbContext;
    private readonly IRequestContextProvider _requestContextProvider;
    private readonly IHubActivityChannel _activityChannel;
    private readonly IHubContext<UserHub> _userHubContext;
    private readonly ILogger<DeviceHub> _logger;

    public DeviceHub(
        DeviceManagerContext dbContext,
        IRequestContextProvider requestContextProvider,
        IHubActivityChannel activityChannel,
        IHubContext<UserHub> userHubContext,
        ILogger<DeviceHub> logger)
    {
        _dbContext = dbContext;
        _requestContextProvider = requestContextProvider;
        _activityChannel = activityChannel;
        _userHubContext = userHubContext;
        _logger = logger;
    }

    /// <summary>
    /// Called when device connects with valid authentication.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        // Populate RequestContext from Hub's ClaimsPrincipal
        var context = _requestContextProvider.Context;
        context.PopulateFromPrincipal(Context.User, _logger);

        var connectionId = Context.ConnectionId;

        _logger.LogInformation("Device connected - ConnectionId: {ConnectionId}, DeviceUserId: {DeviceUserId}, TenantId: {TenantId}, IsDevice: {IsDevice}",
            connectionId, context.UserId, context.TenantId, context.IsDeviceSession);

        // Publish connection activity to channel
        await _activityChannel.PublishAsync(new HubConnectedActivity
        {
            UserId = context.UserId,
            TenantId = context.TenantId,
            ConnectionId = connectionId,
            IsDeviceSession = context.IsDeviceSession,
            HubName = nameof(DeviceHub)
        });

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when device disconnects.
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Populate RequestContext from Hub's ClaimsPrincipal
        var context = _requestContextProvider.Context;
        context.PopulateFromPrincipal(Context.User, _logger);

        var connectionId = Context.ConnectionId;

        if (exception != null)
        {
            _logger.LogWarning(exception, "Device disconnected with error - ConnectionId: {ConnectionId}, DeviceUserId: {DeviceUserId}, TenantId: {TenantId}",
                connectionId, context.UserId, context.TenantId);
        }
        else
        {
            _logger.LogInformation("Device disconnected - ConnectionId: {ConnectionId}, DeviceUserId: {DeviceUserId}, TenantId: {TenantId}",
                connectionId, context.UserId, context.TenantId);
        }

        // Publish disconnection activity to channel
        await _activityChannel.PublishAsync(new HubDisconnectedActivity
        {
            UserId = context.UserId,
            TenantId = context.TenantId,
            ConnectionId = connectionId,
            IsDeviceSession = context.IsDeviceSession,
            HubName = nameof(DeviceHub),
            DisconnectReason = exception?.Message
        });

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Test method to verify device authentication and connectivity.
    /// </summary>
    public string Ping()
    {
        // Populate RequestContext from Hub's ClaimsPrincipal
        var context = _requestContextProvider.Context;
        context.PopulateFromPrincipal(Context.User, _logger);

        _logger.LogInformation("Device ping - DeviceUserId: {DeviceUserId}, TenantId: {TenantId}, RequestId: {RequestId}",
            context.UserId, context.TenantId, context.RequestId);

        return $"Pong from server! DeviceUserId: {context.UserId}, TenantId: {context.TenantId}, RequestId: {context.RequestId}";
    }

    /// <summary>
    /// Device reports its status.
    /// </summary>
    public async Task ReportStatus(string status)
    {
        // Populate RequestContext from Hub's ClaimsPrincipal
        var context = _requestContextProvider.Context;
        context.PopulateFromPrincipal(Context.User, _logger);

        _logger.LogInformation("Device status report - DeviceUserId: {DeviceUserId}, TenantId: {TenantId}, Status: {Status}, RequestId: {RequestId}",
            context.UserId, context.TenantId, status, context.RequestId);

        // Here you could update device status in database
        // Any database queries here will automatically be filtered by tenant
        await Task.CompletedTask;
    }

    /// <summary>
    /// Device sends a ping response back to users.
    /// </summary>
    /// <param name="commandId">The command ID being responded to</param>
    /// <param name="latencyMs">The response latency in milliseconds</param>
    public async Task SendPingResponse(Guid commandId, int latencyMs)
    {
        // Populate RequestContext from Hub's ClaimsPrincipal
        var context = _requestContextProvider.Context;
        context.PopulateFromPrincipal(Context.User, _logger);

        _logger.LogInformation(
            "Device {DeviceId} sent ping response for command {CommandId} with latency {LatencyMs}ms",
            context.UserId, commandId, latencyMs);

        // Forward response to all users in the tenant
        await _userHubContext.Clients
            .Group($"tenant:{context.TenantId}")
            .SendAsync(HubMethodNames.DeviceToUser.ReceivePingResponse, new
            {
                DeviceId = context.UserId,
                CommandId = commandId,
                LatencyMs = latencyMs,
                Timestamp = DateTimeOffset.UtcNow
            });
    }

    /// <summary>
    /// Device acknowledges a command (shutdown, restart, etc).
    /// </summary>
    /// <param name="commandId">The command ID being acknowledged</param>
    /// <param name="commandType">The type of command (shutdown, restart, etc)</param>
    /// <param name="success">Whether the command was executed successfully</param>
    /// <param name="message">Optional message or error details</param>
    public async Task AcknowledgeCommand(Guid commandId, string commandType, bool success, string? message = null)
    {
        // Populate RequestContext from Hub's ClaimsPrincipal
        var context = _requestContextProvider.Context;
        context.PopulateFromPrincipal(Context.User, _logger);

        _logger.LogInformation(
            "Device {DeviceId} acknowledged {CommandType} command {CommandId} - Success: {Success}, Message: {Message}",
            context.UserId, commandType, commandId, success, message);

        // Forward acknowledgment to all users in the tenant
        await _userHubContext.Clients
            .Group($"tenant:{context.TenantId}")
            .SendAsync(HubMethodNames.DeviceToUser.ReceiveCommandAcknowledgment, new
            {
                DeviceId = context.UserId,
                CommandId = commandId,
                CommandType = commandType,
                Success = success,
                Message = message,
                Timestamp = DateTimeOffset.UtcNow
            });
    }
}
