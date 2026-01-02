using DonkeyWork.DeviceManager.Common.Models.Building;
using DonkeyWork.DeviceManager.Common.Models.Room;

namespace DonkeyWork.DeviceManager.Common.Models.Provisioning;

public record ProvisionOrganizationResponse
{
    public required BuildingResponse Building { get; init; }
    public required RoomResponse Room { get; init; }
    public required bool Created { get; init; } // true if created, false if already existed
}
