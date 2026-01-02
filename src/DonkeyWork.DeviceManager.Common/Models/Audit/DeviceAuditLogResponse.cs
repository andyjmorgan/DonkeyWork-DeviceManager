namespace DonkeyWork.DeviceManager.Common.Models.Audit;

/// <summary>
/// Response model for device audit log entries.
/// </summary>
public class DeviceAuditLogResponse
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the device ID.
    /// </summary>
    public Guid DeviceId { get; set; }

    /// <summary>
    /// Gets or sets the device name.
    /// </summary>
    public string DeviceName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of event.
    /// Values: "StatusChange", "Command", "Query"
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the specific action.
    /// For StatusChange: "Online", "Offline"
    /// For Command: "Ping", "Shutdown", "Restart"
    /// For Query: "OSQuery"
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user ID who initiated the action (null for system events).
    /// </summary>
    public Guid? InitiatedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the initiating user's name if available.
    /// </summary>
    public string? InitiatedByUserName { get; set; }

    /// <summary>
    /// Gets or sets additional details about the event (JSON format).
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// Gets or sets the result of the action if applicable.
    /// Values: "Success", "Failure", "Pending", "Timeout"
    /// </summary>
    public string? Result { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when this event occurred.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }
}
