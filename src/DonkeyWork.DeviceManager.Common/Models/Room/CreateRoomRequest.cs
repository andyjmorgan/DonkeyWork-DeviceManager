namespace DonkeyWork.DeviceManager.Common.Models.Room;

public record CreateRoomRequest
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required Guid BuildingId { get; init; }
}
