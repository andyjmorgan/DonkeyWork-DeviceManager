namespace DonkeyWork.DeviceManager.Api.Services.HubActivity;

using DonkeyWork.DeviceManager.Api.Hubs;
using DonkeyWork.DeviceManager.Common.Models.Notifications;
using DonkeyWork.DeviceManager.Common.SignalR;
using DonkeyWork.DeviceManager.Persistence.Context;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

/// <summary>
/// Background service that processes hub activities from the channel.
/// Updates device online status and can be extended for audit logging.
/// </summary>
public class HubActivityProcessor : BackgroundService
{
    private readonly IHubActivityChannel _activityChannel;
    private readonly IServiceProvider _serviceProvider;
    private readonly IHubContext<UserHub> _userHubContext;
    private readonly ILogger<HubActivityProcessor> _logger;

    public HubActivityProcessor(
        IHubActivityChannel activityChannel,
        IServiceProvider serviceProvider,
        IHubContext<UserHub> userHubContext,
        ILogger<HubActivityProcessor> logger)
    {
        _activityChannel = activityChannel;
        _serviceProvider = serviceProvider;
        _userHubContext = userHubContext;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Hub Activity Processor started");

        await foreach (var activity in _activityChannel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessActivityAsync(activity, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing hub activity: {ActivityType}", activity.ActivityType);
            }
        }

        _logger.LogInformation("Hub Activity Processor stopped");
    }

    private async Task ProcessActivityAsync(HubActivity activity, CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Processing hub activity: {ActivityType}, UserId: {UserId}, TenantId: {TenantId}, IsDevice: {IsDevice}",
            activity.ActivityType,
            activity.UserId,
            activity.TenantId,
            activity.IsDeviceSession);

        // Only process device connection activities for online status updates
        if (!activity.IsDeviceSession)
        {
            _logger.LogTrace("Skipping activity - not a device session");
            return;
        }

        // Use a new scope for database operations
        await using var scope = _serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DeviceManagerContext>();

        switch (activity)
        {
            case HubConnectedActivity connected:
                await HandleDeviceConnectedAsync(dbContext, connected, cancellationToken);
                break;

            case HubDisconnectedActivity disconnected:
                await HandleDeviceDisconnectedAsync(dbContext, disconnected, cancellationToken);
                break;

            default:
                _logger.LogTrace("Activity type {ActivityType} does not require device status update", activity.ActivityType);
                break;
        }

        // TODO: Future enhancement - write to audit log table
        // await WriteAuditLogAsync(dbContext, activity, cancellationToken);
    }

    private async Task HandleDeviceConnectedAsync(
        DeviceManagerContext dbContext,
        HubConnectedActivity activity,
        CancellationToken cancellationToken)
    {
        var rowsAffected = await dbContext.Devices
            .IgnoreQueryFilters()
            .Where(d => d.Id == activity.UserId && d.TenantId == activity.TenantId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(d => d.Online, true)
                    .SetProperty(d => d.LastSeen, DateTimeOffset.UtcNow),
                cancellationToken);

        if (rowsAffected > 0)
        {
            _logger.LogInformation(
                "Device {DeviceId} marked as ONLINE in hub {HubName}",
                activity.UserId,
                activity.HubName);

            // Fetch device info and notify users
            var device = await dbContext.Devices
                .IgnoreQueryFilters()
                .Include(d => d.Room)
                    .ThenInclude(r => r.Building)
                .FirstOrDefaultAsync(
                    d => d.Id == activity.UserId && d.TenantId == activity.TenantId,
                    cancellationToken);

            if (device != null)
            {
                var notification = new DeviceStatusNotification
                {
                    DeviceId = device.Id,
                    DeviceName = device.Name,
                    Online = true,
                    Timestamp = DateTimeOffset.UtcNow,
                    RoomName = device.Room?.Name,
                    BuildingName = device.Room?.Building?.Name
                };

                // Send notification to all users in the tenant
                await _userHubContext.Clients
                    .Group($"tenant:{activity.TenantId}")
                    .SendAsync(HubMethodNames.DeviceToUser.ReceiveDeviceStatus, notification, cancellationToken);

                _logger.LogInformation(
                    "Sent online notification for device {DeviceId} to tenant {TenantId}",
                    device.Id, activity.TenantId);
            }
        }
        else
        {
            _logger.LogWarning(
                "Device not found for DeviceId {DeviceId} in TenantId {TenantId}",
                activity.UserId,
                activity.TenantId);
        }
    }

    private async Task HandleDeviceDisconnectedAsync(
        DeviceManagerContext dbContext,
        HubDisconnectedActivity activity,
        CancellationToken cancellationToken)
    {
        var rowsAffected = await dbContext.Devices
            .IgnoreQueryFilters()
            .Where(d => d.Id == activity.UserId && d.TenantId == activity.TenantId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(d => d.Online, false),
                cancellationToken);

        if (rowsAffected > 0)
        {
            _logger.LogInformation(
                "Device {DeviceId} marked as OFFLINE in hub {HubName}. Reason: {Reason}",
                activity.UserId,
                activity.HubName,
                activity.DisconnectReason ?? "Unknown");

            // Fetch device info and notify users
            var device = await dbContext.Devices
                .IgnoreQueryFilters()
                .Include(d => d.Room)
                    .ThenInclude(r => r.Building)
                .FirstOrDefaultAsync(
                    d => d.Id == activity.UserId && d.TenantId == activity.TenantId,
                    cancellationToken);

            if (device != null)
            {
                var notification = new DeviceStatusNotification
                {
                    DeviceId = device.Id,
                    DeviceName = device.Name,
                    Online = false,
                    Timestamp = DateTimeOffset.UtcNow,
                    RoomName = device.Room?.Name,
                    BuildingName = device.Room?.Building?.Name
                };

                // Send notification to all users in the tenant
                await _userHubContext.Clients
                    .Group($"tenant:{activity.TenantId}")
                    .SendAsync(HubMethodNames.DeviceToUser.ReceiveDeviceStatus, notification, cancellationToken);

                _logger.LogInformation(
                    "Sent offline notification for device {DeviceId} to tenant {TenantId}",
                    device.Id, activity.TenantId);
            }
        }
        else
        {
            _logger.LogWarning(
                "Device not found for DeviceId {DeviceId} in TenantId {TenantId}",
                activity.UserId,
                activity.TenantId);
        }
    }
}
