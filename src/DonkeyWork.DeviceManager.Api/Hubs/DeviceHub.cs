namespace DonkeyWork.DeviceManager.Api.Hubs;

using DonkeyWork.DeviceManager.Api.Authorization;
using DonkeyWork.DeviceManager.Api.Services;
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
/// Uses strongly-typed client interface for sending notifications to users.
/// </summary>
[Authorize(Policy = AuthorizationPolicies.DeviceOnly)]
public class DeviceHub : Hub
{
    private readonly DeviceManagerContext _dbContext;
    private readonly IRequestContextProvider _requestContextProvider;
    private readonly IHubActivityChannel _activityChannel;
    private readonly IHubContext<UserHub, IUserClient> _userHubContext;
    private readonly IOSQueryService _osqueryService;
    private readonly IDeviceAuditService _auditService;
    private readonly ILogger<DeviceHub> _logger;

    public DeviceHub(
        DeviceManagerContext dbContext,
        IRequestContextProvider requestContextProvider,
        IHubActivityChannel activityChannel,
        IHubContext<UserHub, IUserClient> userHubContext,
        IOSQueryService osqueryService,
        IDeviceAuditService auditService,
        ILogger<DeviceHub> logger)
    {
        _dbContext = dbContext;
        _requestContextProvider = requestContextProvider;
        _activityChannel = activityChannel;
        _userHubContext = userHubContext;
        _osqueryService = osqueryService;
        _auditService = auditService;
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

        // Log device online status to audit trail
        await _auditService.LogStatusChangeAsync(context.UserId, true);

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

        // Log device offline status to audit trail
        await _auditService.LogStatusChangeAsync(context.UserId, false);

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
    /// Device reports its status (for future use).
    /// </summary>
    public async Task ReportStatus(string status)
    {
        // Populate RequestContext from Hub's ClaimsPrincipal
        var context = _requestContextProvider.Context;
        context.PopulateFromPrincipal(Context.User, _logger);

        _logger.LogInformation("Device status report - DeviceUserId: {DeviceUserId}, TenantId: {TenantId}, Status: {Status}, RequestId: {RequestId}",
            context.UserId, context.TenantId, status, context.RequestId);

        // Broadcast status to all users in tenant
        await _userHubContext.Clients
            .Group($"tenant:{context.TenantId}")
            .ReceiveDeviceStatus(new DeviceStatus
            {
                DeviceId = context.UserId,
                IsOnline = true,
                Timestamp = DateTimeOffset.UtcNow,
                Reason = status
            });
    }
}
