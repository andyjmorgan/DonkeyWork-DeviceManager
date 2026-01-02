namespace DonkeyWork.DeviceManager.Common.Models.Building;

public record UpdateBuildingRequest
{
    public required string Name { get; init; }
    public string? Description { get; init; }
}
