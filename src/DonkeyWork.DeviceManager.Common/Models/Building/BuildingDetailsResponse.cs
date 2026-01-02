using DonkeyWork.DeviceManager.Common.Models.Room;

namespace DonkeyWork.DeviceManager.Common.Models.Building;

public record BuildingDetailsResponse
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required List<RoomResponse> Rooms { get; init; }
    public required DateTimeOffset Created { get; init; }
    public required DateTimeOffset Updated { get; init; }
}
