namespace DonkeyWork.DeviceManager.Persistence.Entity;

using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;
using DonkeyWork.DeviceManager.Common.Models.DeviceInformation;
using DonkeyWork.DeviceManager.Persistence.Entity.Base;

public class DeviceEntity : BaseAuditEntity
{
    /// <summary>
    /// Gets or sets the device identifier (Keycloak user ID).
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the device name.
    /// </summary>
    [MaxLength(128)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the device description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the last seen timestamp.
    /// </summary>
    public DateTimeOffset LastSeen { get; set; }

    /// <summary>
    /// Gets or sets whether the device is currently online (connected to hub).
    /// </summary>
    public bool Online { get; set; }

    /// <summary>
    /// Gets or sets the room this device is located in.
    /// </summary>
    public virtual RoomEntity Room { get; set; }

    // Hardware Information

    /// <summary>
    /// Gets or sets the number of CPU cores.
    /// </summary>
    public int? CpuCores { get; set; }

    /// <summary>
    /// Gets or sets the total memory in bytes.
    /// </summary>
    public long? TotalMemoryBytes { get; set; }

    /// <summary>
    /// Gets or sets the operating system type.
    /// </summary>
    public OperatingSystemType? OperatingSystem { get; set; }

    /// <summary>
    /// Gets or sets the OS architecture.
    /// </summary>
    public Architecture? OSArchitecture { get; set; }

    /// <summary>
    /// Gets or sets the process architecture.
    /// </summary>
    public Architecture? Architecture { get; set; }

    /// <summary>
    /// Gets or sets the operating system version.
    /// </summary>
    [MaxLength(256)]
    public string? OperatingSystemVersion { get; set; }
}