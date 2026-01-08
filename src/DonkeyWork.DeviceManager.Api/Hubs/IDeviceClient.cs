namespace DonkeyWork.DeviceManager.Api.Hubs;

/// <summary>
/// Strongly-typed interface for methods that can be invoked on device clients.
/// Used by UserHub to send commands and request responses from devices.
/// All methods use modern SignalR patterns (request-response or streaming).
/// </summary>
public interface IDeviceClient
{
    /// <summary>
    /// Requests a ping measurement from the device.
    /// Device should measure round-trip time and return latency in milliseconds.
    /// </summary>
    /// <param name="commandId">Command identifier for tracking</param>
    /// <param name="timestamp">When the command was sent</param>
    /// <param name="requestedBy">User ID who requested the ping</param>
    /// <returns>Latency in milliseconds</returns>
    Task<int> MeasurePing(Guid commandId, DateTimeOffset timestamp, Guid requestedBy);

    /// <summary>
    /// Requests the device to initiate a shutdown.
    /// </summary>
    /// <param name="commandId">Command identifier for tracking</param>
    /// <param name="timestamp">When the command was sent</param>
    /// <param name="requestedBy">User ID who requested the shutdown</param>
    /// <returns>Command execution result</returns>
    Task<CommandResult> ExecuteShutdown(Guid commandId, DateTimeOffset timestamp, Guid requestedBy);

    /// <summary>
    /// Requests the device to restart.
    /// </summary>
    /// <param name="commandId">Command identifier for tracking</param>
    /// <param name="timestamp">When the command was sent</param>
    /// <param name="requestedBy">User ID who requested the restart</param>
    /// <returns>Command execution result</returns>
    Task<CommandResult> ExecuteRestart(Guid commandId, DateTimeOffset timestamp, Guid requestedBy);

    /// <summary>
    /// Requests the device to execute an OSQuery and stream results back.
    /// </summary>
    /// <param name="executionId">Execution identifier for tracking</param>
    /// <param name="query">SQL query to execute</param>
    /// <param name="timestamp">When the command was sent</param>
    /// <param name="requestedBy">User ID who requested the query</param>
    /// <returns>Async stream of query result rows</returns>
    IAsyncEnumerable<OSQueryResultRow> ExecuteStreamingOSQuery(Guid executionId, string query, DateTimeOffset timestamp, Guid requestedBy);
}

/// <summary>
/// Result of a command execution on a device.
/// </summary>
public class CommandResult
{
    /// <summary>
    /// Whether the command executed successfully.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Optional message or error details.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// When the command completed execution.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Single row from an OSQuery result set.
/// </summary>
public class OSQueryResultRow
{
    /// <summary>
    /// Row data as JSON.
    /// </summary>
    public string RowJson { get; set; } = string.Empty;

    /// <summary>
    /// Row number in the result set.
    /// </summary>
    public int RowNumber { get; set; }
}
