namespace DonkeyWork.DeviceManager.Common.Models.Building;

public record BuildingResponse
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required int RoomCount { get; init; }
    public required DateTimeOffset Created { get; init; }
    public required DateTimeOffset Updated { get; init; }
}
