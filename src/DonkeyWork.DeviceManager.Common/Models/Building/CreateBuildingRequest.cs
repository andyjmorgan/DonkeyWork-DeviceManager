namespace DonkeyWork.DeviceManager.Common.Models.Building;

public record CreateBuildingRequest
{
    public required string Name { get; init; }
    public string? Description { get; init; }
}
