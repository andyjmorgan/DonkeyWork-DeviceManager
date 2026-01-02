namespace DonkeyWork.DeviceManager.Common.Models.Registration;

/// <summary>
/// Building with rooms.
/// </summary>
public record BuildingResponse
{
    /// <summary>
    /// Gets the building ID.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets the building name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the list of rooms in this building.
    /// </summary>
    public required List<RoomResponse> Rooms { get; init; }
}
