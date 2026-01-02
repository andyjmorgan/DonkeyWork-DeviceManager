namespace DonkeyWork.DeviceManager.Common.Models.Room;

public record RoomResponse
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required Guid BuildingId { get; init; }
    public required string BuildingName { get; init; }
    public required int DeviceCount { get; init; }
    public required DateTimeOffset Created { get; init; }
    public required DateTimeOffset Updated { get; init; }
}
