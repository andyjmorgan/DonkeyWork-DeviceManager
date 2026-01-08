namespace DonkeyWork.DeviceManager.Api.Hubs;

/// <summary>
/// Strongly-typed interface for methods that can be invoked on user clients.
/// Used by DeviceHub to send notifications and responses to frontend users.
/// </summary>
public interface IUserClient
{
    /// <summary>
    /// Notifies user of a ping response from a device.
    /// </summary>
    /// <param name="response">Ping response details</param>
    Task ReceivePingResponse(PingResponse response);

    /// <summary>
    /// Notifies user of command acknowledgment from a device.
    /// </summary>
    /// <param name="acknowledgment">Command acknowledgment details</param>
    Task ReceiveCommandAcknowledgment(CommandAcknowledgment acknowledgment);

    /// <summary>
    /// Notifies user of device status change (online/offline).
    /// </summary>
    /// <param name="status">Device status details</param>
    Task ReceiveDeviceStatus(DeviceStatus status);

    /// <summary>
    /// Notifies user of OSQuery result from a device.
    /// </summary>
    /// <param name="result">OSQuery result details</param>
    Task ReceiveOSQueryResult(OSQueryResult result);
}

/// <summary>
/// Ping response from a device.
/// </summary>
public class PingResponse
{
    /// <summary>
    /// Device that responded to the ping.
    /// </summary>
    public Guid DeviceId { get; set; }

    /// <summary>
    /// Command ID that was responded to.
    /// </summary>
    public Guid CommandId { get; set; }

    /// <summary>
    /// Measured latency in milliseconds.
    /// </summary>
    public int LatencyMs { get; set; }

    /// <summary>
    /// When the response was sent.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }
}

/// <summary>
/// Command acknowledgment from a device.
/// </summary>
public class CommandAcknowledgment
{
    /// <summary>
    /// Device that acknowledged the command.
    /// </summary>
    public Guid DeviceId { get; set; }

    /// <summary>
    /// Command ID that was acknowledged.
    /// </summary>
    public Guid CommandId { get; set; }

    /// <summary>
    /// Type of command (shutdown, restart, etc).
    /// </summary>
    public string CommandType { get; set; } = string.Empty;

    /// <summary>
    /// Whether the command executed successfully.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Optional message or error details.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// When the acknowledgment was sent.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }
}

/// <summary>
/// Device status change notification.
/// </summary>
public class DeviceStatus
{
    /// <summary>
    /// Device whose status changed.
    /// </summary>
    public Guid DeviceId { get; set; }

    /// <summary>
    /// Whether the device is online.
    /// </summary>
    public bool IsOnline { get; set; }

    /// <summary>
    /// When the status changed.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Optional reason for status change.
    /// </summary>
    public string? Reason { get; set; }
}

/// <summary>
/// OSQuery result from a device.
/// </summary>
public class OSQueryResult
{
    /// <summary>
    /// Device that executed the query.
    /// </summary>
    public Guid DeviceId { get; set; }

    /// <summary>
    /// Execution ID for tracking.
    /// </summary>
    public Guid ExecutionId { get; set; }

    /// <summary>
    /// Whether the query executed successfully.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if query failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Raw JSON result from osquery.
    /// </summary>
    public string? RawJson { get; set; }

    /// <summary>
    /// Execution time in milliseconds.
    /// </summary>
    public int ExecutionTimeMs { get; set; }

    /// <summary>
    /// Number of rows returned.
    /// </summary>
    public int RowCount { get; set; }

    /// <summary>
    /// When the result was sent.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }
}
