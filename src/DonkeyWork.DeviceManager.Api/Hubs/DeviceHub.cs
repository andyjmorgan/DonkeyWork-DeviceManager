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

        // Forward response to all users in the tenant using strongly-typed interface
        await _userHubContext.Clients
            .Group($"tenant:{context.TenantId}")
            .ReceivePingResponse(new PingResponse
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

        // Forward acknowledgment to all users in the tenant using strongly-typed interface
        await _userHubContext.Clients
            .Group($"tenant:{context.TenantId}")
            .ReceiveCommandAcknowledgment(new CommandAcknowledgment
            {
                DeviceId = context.UserId,
                CommandId = commandId,
                CommandType = commandType,
                Success = success,
                Message = message,
                Timestamp = DateTimeOffset.UtcNow
            });
    }

    /// <summary>
    /// Device sends OSQuery result back to users.
    /// </summary>
    /// <param name="executionId">The execution ID</param>
    /// <param name="success">Whether the query executed successfully</param>
    /// <param name="errorMessage">Error message if query failed</param>
    /// <param name="rawJson">The JSON result from osquery</param>
    /// <param name="executionTimeMs">Execution time in milliseconds</param>
    /// <param name="rowCount">Number of rows returned</param>
    public async Task SendOSQueryResult(Guid executionId, bool success, string? errorMessage, string? rawJson, int executionTimeMs, int rowCount)
    {
        // Populate RequestContext from Hub's ClaimsPrincipal
        var context = _requestContextProvider.Context;
        context.PopulateFromPrincipal(Context.User, _logger);

        var deviceId = context.UserId;

        _logger.LogInformation(
            "Device {DeviceId} sent OSQuery result for execution {ExecutionId} - Success: {Success}, Rows: {RowCount}, Time: {ExecutionTimeMs}ms",
            deviceId, executionId, success, rowCount, executionTimeMs);

        try
        {
            // Save result to database
            await _osqueryService.SaveExecutionResultAsync(
                executionId,
                deviceId,
                success,
                errorMessage,
                rawJson,
                executionTimeMs,
                rowCount);

            // Update execution counts
            await _osqueryService.UpdateExecutionCountsAsync(executionId);

            // Log OSQuery execution to audit trail
            await _auditService.LogOSQueryAsync(
                deviceId,
                context.UserId, // In this case, userId is the device ID
                "OSQuery execution",
                success,
                errorMessage);

            // Forward result to all users in the tenant using strongly-typed interface
            await _userHubContext.Clients
                .Group($"tenant:{context.TenantId}")
                .ReceiveOSQueryResult(new OSQueryResult
                {
                    DeviceId = deviceId,
                    ExecutionId = executionId,
                    Success = success,
                    ErrorMessage = errorMessage,
                    RawJson = rawJson,
                    ExecutionTimeMs = executionTimeMs,
                    RowCount = rowCount,
                    Timestamp = DateTimeOffset.UtcNow
                });

            _logger.LogInformation(
                "OSQuery result for execution {ExecutionId} forwarded to tenant {TenantId}",
                executionId, context.TenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to process OSQuery result from device {DeviceId} for execution {ExecutionId}",
                deviceId, executionId);
            throw;
        }
    }
}
