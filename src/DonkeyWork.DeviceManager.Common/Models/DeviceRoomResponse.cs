namespace DonkeyWork.DeviceManager.Common.Models;

/// <summary>
/// Room information for a device, including its building.
/// </summary>
public record DeviceRoomResponse
{
    /// <summary>
    /// Gets the room ID.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets the room name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the building information where the room is located.
    /// </summary>
    public required DeviceBuildingResponse Building { get; init; }
}
