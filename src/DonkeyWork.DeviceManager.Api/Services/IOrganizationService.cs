using DonkeyWork.DeviceManager.Common.Models.Building;
using DonkeyWork.DeviceManager.Common.Models.Provisioning;
using DonkeyWork.DeviceManager.Common.Models.Room;

namespace DonkeyWork.DeviceManager.Api.Services;

public interface IOrganizationService
{
    // Building operations
    Task<List<BuildingResponse>> GetBuildingsAsync();
    Task<BuildingDetailsResponse?> GetBuildingByIdAsync(Guid id);
    Task<BuildingResponse> CreateBuildingAsync(CreateBuildingRequest request);
    Task<BuildingResponse?> UpdateBuildingAsync(Guid id, UpdateBuildingRequest request);
    Task<bool> DeleteBuildingAsync(Guid id);

    // Room operations
    Task<List<RoomResponse>> GetRoomsAsync(Guid? buildingId = null);
    Task<RoomDetailsResponse?> GetRoomByIdAsync(Guid id);
    Task<RoomResponse> CreateRoomAsync(CreateRoomRequest request);
    Task<RoomResponse?> UpdateRoomAsync(Guid id, UpdateRoomRequest request);
    Task<bool> DeleteRoomAsync(Guid id);

    // Provisioning operations
    Task<ProvisionOrganizationResponse> EnsureOrganizationStructureAsync();
}
