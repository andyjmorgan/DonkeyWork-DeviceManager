namespace DonkeyWork.DeviceManager.Persistence.Entity;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// An ephemeral entity representing a device registration.
/// </summary>
public class DeviceRegistrationEntity
{
    /// <summary>
    /// Gets or sets the device registration identifier.
    /// </summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the SignalR connection identifier.
    /// </summary>
    [MaxLength(64)]
    public string ConnectionId { get; set; }

    /// <summary>
    /// Gets or sets the three-word registration identifier.
    /// </summary>
    [MaxLength(64)]
    public string ThreeWordRegistration { get; set; }
}