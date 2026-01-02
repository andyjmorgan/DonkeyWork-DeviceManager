namespace DonkeyWork.DeviceManager.Common.Models.Registration;

/// <summary>
/// Room information.
/// </summary>
public record RoomResponse
{
    /// <summary>
    /// Gets the room ID.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets the room name.
    /// </summary>
    public required string Name { get; init; }
}
