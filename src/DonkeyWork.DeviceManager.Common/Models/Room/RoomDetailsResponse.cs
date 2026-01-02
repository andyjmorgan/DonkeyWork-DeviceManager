using DonkeyWork.DeviceManager.Common.Models.Building;
using DonkeyWork.DeviceManager.Common.Models.Device;

namespace DonkeyWork.DeviceManager.Common.Models.Room;

public record RoomDetailsResponse
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required BuildingResponse Building { get; init; }
    public required List<DeviceResponse> Devices { get; init; }
    public required DateTimeOffset Created { get; init; }
    public required DateTimeOffset Updated { get; init; }
}
