namespace DonkeyWork.DeviceManager.Api.Services.HubActivity;

/// <summary>
/// Base class for all hub activities that can be logged and processed.
/// </summary>
public abstract record HubActivity
{
    /// <summary>
    /// Gets the timestamp when the activity occurred.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets the user ID associated with the activity.
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// Gets the tenant ID associated with the activity.
    /// </summary>
    public Guid TenantId { get; init; }

    /// <summary>
    /// Gets the connection ID from SignalR.
    /// </summary>
    public string ConnectionId { get; init; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether this is a device session.
    /// </summary>
    public bool IsDeviceSession { get; init; }

    /// <summary>
    /// Gets the activity type name for logging.
    /// </summary>
    public abstract string ActivityType { get; }
}

/// <summary>
/// Activity when a client connects to a hub.
/// </summary>
public record HubConnectedActivity : HubActivity
{
    public override string ActivityType => "Connected";

    /// <summary>
    /// Gets the name of the hub that was connected to.
    /// </summary>
    public string HubName { get; init; } = string.Empty;
}

/// <summary>
/// Activity when a client disconnects from a hub.
/// </summary>
public record HubDisconnectedActivity : HubActivity
{
    public override string ActivityType => "Disconnected";

    /// <summary>
    /// Gets the name of the hub that was disconnected from.
    /// </summary>
    public string HubName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the reason for disconnection if available.
    /// </summary>
    public string? DisconnectReason { get; init; }
}

/// <summary>
/// Activity when a hub method is invoked.
/// </summary>
public record HubMethodInvokedActivity : HubActivity
{
    public override string ActivityType => "MethodInvoked";

    /// <summary>
    /// Gets the name of the hub.
    /// </summary>
    public string HubName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the name of the method that was invoked.
    /// </summary>
    public string MethodName { get; init; } = string.Empty;

    /// <summary>
    /// Gets optional metadata about the method invocation.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; init; }
}

/// <summary>
/// Activity for general hub events that should be audited.
/// </summary>
public record HubEventActivity : HubActivity
{
    public override string ActivityType => "Event";

    /// <summary>
    /// Gets the name of the hub.
    /// </summary>
    public string HubName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the event name.
    /// </summary>
    public string EventName { get; init; } = string.Empty;

    /// <summary>
    /// Gets optional event data.
    /// </summary>
    public Dictionary<string, object>? EventData { get; init; }
}
