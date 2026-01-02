namespace DonkeyWork.DeviceManager.Common.Models;

using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using DonkeyWork.DeviceManager.Common.Models.DeviceInformation;

/// <summary>
/// Device information including room and building details.
/// </summary>
public record DeviceResponse
{
    /// <summary>
    /// Gets the device ID (Keycloak user ID).
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets the device name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the device description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets whether the device is currently online.
    /// </summary>
    public required bool Online { get; init; }

    /// <summary>
    /// Gets the time the device was created.
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Gets the last time the device was seen.
    /// </summary>
    public required DateTimeOffset LastSeen { get; init; }

    /// <summary>
    /// Gets the room information where the device is located.
    /// </summary>
    public required DeviceRoomResponse Room { get; init; }

    // Hardware Information

    /// <summary>
    /// Gets the number of CPU cores.
    /// </summary>
    public int? CpuCores { get; init; }

    /// <summary>
    /// Gets the total memory in bytes.
    /// </summary>
    public long? TotalMemoryBytes { get; init; }

    /// <summary>
    /// Gets the operating system type.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public OperatingSystemType? OperatingSystem { get; init; }

    /// <summary>
    /// Gets the OS architecture.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Architecture? OSArchitecture { get; init; }

    /// <summary>
    /// Gets the process architecture.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Architecture? Architecture { get; init; }

    /// <summary>
    /// Gets the operating system version.
    /// </summary>
    public string? OperatingSystemVersion { get; init; }
}
