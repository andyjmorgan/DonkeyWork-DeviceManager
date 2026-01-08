namespace DonkeyWork.DeviceManager.Api.Hubs;

/// <summary>
/// Strongly-typed interface for methods that can be invoked on user clients.
/// Used by DeviceHub to send notifications to frontend users.
/// For command responses, use the request-response pattern methods on UserHub instead.
/// </summary>
public interface IUserClient
{
    /// <summary>
    /// Notifies user of device status change (online/offline).
    /// </summary>
    /// <param name="status">Device status details</param>
    Task ReceiveDeviceStatus(DeviceStatus status);
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
