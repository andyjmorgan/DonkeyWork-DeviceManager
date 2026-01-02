namespace DonkeyWork.DeviceManager.Api.Hubs;

using DonkeyWork.DeviceManager.Api.Services;
using DonkeyWork.DeviceManager.Backend.Common.RequestContextServices;
using DonkeyWork.DeviceManager.Common.SignalR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

/// <summary>
/// SignalR hub for frontend user connections.
/// Users receive real-time notifications about device status changes
/// and can send commands to devices.
/// </summary>
[Authorize(Policy = "UserOnly")]
public class UserHub : Hub
{
    private readonly IRequestContextProvider _requestContextProvider;
    private readonly IHubContext<DeviceHub> _deviceHubContext;
    private readonly IDeviceAuditService _auditService;
    private readonly IOSQueryService _osqueryService;
    private readonly ILogger<UserHub> _logger;

    public UserHub(
        IRequestContextProvider requestContextProvider,
        IHubContext<DeviceHub> deviceHubContext,
        IDeviceAuditService auditService,
        IOSQueryService osqueryService,
        ILogger<UserHub> logger)
    {
        _requestContextProvider = requestContextProvider;
        _deviceHubContext = deviceHubContext;
        _auditService = auditService;
        _osqueryService = osqueryService;
        _logger = logger;
    }

    /// <summary>
    /// Called when a user connects to the hub.
    /// Adds the user to their tenant group for targeted notifications.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        // Populate RequestContext from Hub's ClaimsPrincipal
        var context = _requestContextProvider.Context;
        context.PopulateFromPrincipal(Context.User, _logger);

        var connectionId = Context.ConnectionId;
        var tenantId = context.TenantId;
        var userId = context.UserId;

        _logger.LogInformation(
            "User connected - ConnectionId: {ConnectionId}, UserId: {UserId}, TenantId: {TenantId}",
            connectionId, userId, tenantId);

        // Add user to tenant group for targeted notifications
        await Groups.AddToGroupAsync(connectionId, $"tenant:{tenantId}");

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a user disconnects from the hub.
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var context = _requestContextProvider.Context;
        context.PopulateFromPrincipal(Context.User, _logger);

        var connectionId = Context.ConnectionId;
        var tenantId = context.TenantId;

        _logger.LogInformation(
            "User disconnected - ConnectionId: {ConnectionId}, TenantId: {TenantId}",
            connectionId, tenantId);

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Sends a ping command to a specific device.
    /// The device will respond via SignalR.
    /// </summary>
    /// <param name="deviceId">The device ID to ping</param>
    public async Task PingDevice(Guid deviceId)
    {
        var context = _requestContextProvider.Context;
        context.PopulateFromPrincipal(Context.User, _logger);

        var commandId = Guid.NewGuid();

        _logger.LogInformation(
            "User {UserId} sending ping command {CommandId} to device {DeviceId} (using SignalR UserId: {SignalRUserId})",
            context.UserId, commandId, deviceId, deviceId.ToString());

        try
        {
            // Log command to audit trail
            await _auditService.LogCommandAsync(deviceId, "Ping", context.UserId);

            // Send command to device's connection
            await _deviceHubContext.Clients
                .User(deviceId.ToString())
                .SendAsync(HubMethodNames.UserToDevice.ReceivePingCommand, new
                {
                    CommandId = commandId,
                    Timestamp = DateTimeOffset.UtcNow,
                    RequestedBy = context.UserId
                });

            _logger.LogInformation(
                "Ping command {CommandId} sent to device {DeviceId} from user {UserId}",
                commandId, deviceId, context.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send ping command to device {DeviceId}",
                deviceId);
            throw;
        }
    }

    /// <summary>
    /// Sends a shutdown command to a specific device.
    /// The device will initiate shutdown and disconnect.
    /// </summary>
    /// <param name="deviceId">The device ID to shutdown</param>
    public async Task ShutdownDevice(Guid deviceId)
    {
        var context = _requestContextProvider.Context;
        context.PopulateFromPrincipal(Context.User, _logger);

        var commandId = Guid.NewGuid();

        _logger.LogInformation(
            "User {UserId} sending shutdown command {CommandId} to device {DeviceId}",
            context.UserId, commandId, deviceId);

        try
        {
            // Log command to audit trail
            await _auditService.LogCommandAsync(deviceId, "Shutdown", context.UserId);

            // Send command to device's connection
            await _deviceHubContext.Clients
                .User(deviceId.ToString())
                .SendAsync(HubMethodNames.UserToDevice.ReceiveShutdownCommand, new
                {
                    CommandId = commandId,
                    Timestamp = DateTimeOffset.UtcNow,
                    RequestedBy = context.UserId
                });

            _logger.LogInformation(
                "Shutdown command {CommandId} sent to device {DeviceId} from user {UserId}",
                commandId, deviceId, context.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send shutdown command to device {DeviceId}",
                deviceId);
            throw;
        }
    }

    /// <summary>
    /// Sends a restart command to a specific device.
    /// The device will restart its operating system.
    /// </summary>
    /// <param name="deviceId">The device ID to restart</param>
    public async Task RestartDevice(Guid deviceId)
    {
        var context = _requestContextProvider.Context;
        context.PopulateFromPrincipal(Context.User, _logger);

        var commandId = Guid.NewGuid();

        _logger.LogInformation(
            "User {UserId} sending restart command {CommandId} to device {DeviceId}",
            context.UserId, commandId, deviceId);

        try
        {
            // Log command to audit trail
            await _auditService.LogCommandAsync(deviceId, "Restart", context.UserId);

            // Send command to device's connection
            await _deviceHubContext.Clients
                .User(deviceId.ToString())
                .SendAsync(HubMethodNames.UserToDevice.ReceiveRestartCommand, new
                {
                    CommandId = commandId,
                    Timestamp = DateTimeOffset.UtcNow,
                    RequestedBy = context.UserId
                });

            _logger.LogInformation(
                "Restart command {CommandId} sent to device {DeviceId} from user {UserId}",
                commandId, deviceId, context.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send restart command to device {DeviceId}",
                deviceId);
            throw;
        }
    }

    /// <summary>
    /// Sends an OSQuery command to a specific device.
    /// The device will execute the query and return results via SendOSQueryResult.
    /// </summary>
    /// <param name="deviceId">The device ID to query</param>
    /// <param name="query">The SQL query to execute</param>
    /// <param name="executionId">The execution ID for tracking results</param>
    public async Task ExecuteOSQuery(Guid deviceId, string query, Guid executionId)
    {
        var context = _requestContextProvider.Context;
        context.PopulateFromPrincipal(Context.User, _logger);

        _logger.LogInformation(
            "User {UserId} sending OSQuery command (execution {ExecutionId}) to device {DeviceId}",
            context.UserId, executionId, deviceId);

        try
        {
            // Send command to device's connection
            await _deviceHubContext.Clients
                .User(deviceId.ToString())
                .SendAsync(HubMethodNames.UserToDevice.ReceiveOSQueryCommand, new
                {
                    ExecutionId = executionId,
                    Query = query,
                    Timestamp = DateTimeOffset.UtcNow,
                    RequestedBy = context.UserId
                });

            _logger.LogInformation(
                "OSQuery command (execution {ExecutionId}) sent to device {DeviceId} from user {UserId}",
                executionId, deviceId, context.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send OSQuery command to device {DeviceId}",
                deviceId);
            throw;
        }
    }
}
