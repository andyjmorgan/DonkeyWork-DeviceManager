namespace DonkeyWork.DeviceManager.Common.Models.Commands;

/// <summary>
/// Data structure for commands sent from users to devices via SignalR.
/// </summary>
public record CommandData
{
    public Guid CommandId { get; init; }
    public DateTimeOffset Timestamp { get; init; }
    public Guid RequestedBy { get; init; }
}

/// <summary>
/// Data structure for ping commands.
/// </summary>
public record PingCommandData : CommandData
{
}

/// <summary>
/// Data structure for ping responses from devices.
/// </summary>
public record PingResponseData
{
    public Guid DeviceId { get; init; }
    public Guid CommandId { get; init; }
    public int LatencyMs { get; init; }
    public DateTimeOffset Timestamp { get; init; }
}

/// <summary>
/// Data structure for command acknowledgments from devices.
/// </summary>
public record CommandAcknowledgmentData
{
    public Guid DeviceId { get; init; }
    public Guid CommandId { get; init; }
    public string CommandType { get; init; } = string.Empty;
    public bool Success { get; init; }
    public string? Message { get; init; }
    public DateTimeOffset Timestamp { get; init; }
}

/// <summary>
/// Data structure for OSQuery commands sent to devices.
/// </summary>
public record OSQueryCommandData
{
    public Guid ExecutionId { get; init; }
    public string Query { get; init; } = string.Empty;
    public DateTimeOffset Timestamp { get; init; }
    public Guid RequestedBy { get; init; }
}
