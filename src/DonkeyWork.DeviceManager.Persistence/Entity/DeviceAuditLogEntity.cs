namespace DonkeyWork.DeviceManager.Persistence.Entity;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Represents an audit log entry for device events and actions.
/// Tracks device online/offline status changes and commands sent to devices.
/// </summary>
public class DeviceAuditLogEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for this audit log entry.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the device ID this audit entry is for.
    /// </summary>
    public Guid DeviceId { get; set; }

    /// <summary>
    /// Gets or sets the device this audit entry is for.
    /// </summary>
    public virtual DeviceEntity Device { get; set; } = null!;

    /// <summary>
    /// Gets or sets the type of event.
    /// Values: "StatusChange", "Command", "Query"
    /// </summary>
    [MaxLength(50)]
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the specific action.
    /// For StatusChange: "Online", "Offline"
    /// For Command: "Ping", "Shutdown", "Restart", "OSQuery"
    /// For Query: "OSQuery"
    /// </summary>
    [MaxLength(50)]
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user ID who initiated the action (null for system events).
    /// </summary>
    public Guid? InitiatedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the tenant ID for multi-tenancy.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Gets or sets additional details about the event (JSON format).
    /// Can include command parameters, query text, error messages, etc.
    /// </summary>
    [MaxLength(4000)]
    public string? Details { get; set; }

    /// <summary>
    /// Gets or sets the result of the action if applicable.
    /// Values: "Success", "Failure", "Pending", "Timeout"
    /// </summary>
    [MaxLength(50)]
    public string? Result { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when this event occurred.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }
}
