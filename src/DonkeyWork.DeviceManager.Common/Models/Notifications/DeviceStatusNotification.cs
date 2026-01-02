namespace DonkeyWork.DeviceManager.Common.Models.Notifications;

/// <summary>
/// Notification sent to users when a device status changes.
/// </summary>
public record DeviceStatusNotification
{
    /// <summary>
    /// Gets the device ID.
    /// </summary>
    public required Guid DeviceId { get; init; }

    /// <summary>
    /// Gets the device name.
    /// </summary>
    public required string DeviceName { get; init; }

    /// <summary>
    /// Gets whether the device is online.
    /// </summary>
    public required bool Online { get; init; }

    /// <summary>
    /// Gets the timestamp of the status change.
    /// </summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Gets the room where the device is located (optional).
    /// </summary>
    public string? RoomName { get; init; }

    /// <summary>
    /// Gets the building where the device is located (optional).
    /// </summary>
    public string? BuildingName { get; init; }
}
