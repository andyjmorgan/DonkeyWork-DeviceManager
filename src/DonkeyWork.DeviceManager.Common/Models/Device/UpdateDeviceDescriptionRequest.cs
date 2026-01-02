namespace DonkeyWork.DeviceManager.Common.Models.Device;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Request model for updating a device's description.
/// </summary>
public class UpdateDeviceDescriptionRequest
{
    /// <summary>
    /// Gets or sets the new description for the device.
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }
}
