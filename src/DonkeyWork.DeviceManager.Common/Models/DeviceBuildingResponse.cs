namespace DonkeyWork.DeviceManager.Common.Models;

/// <summary>
/// Building information for a device.
/// </summary>
public record DeviceBuildingResponse
{
    /// <summary>
    /// Gets the building ID.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets the building name.
    /// </summary>
    public required string Name { get; init; }
}
