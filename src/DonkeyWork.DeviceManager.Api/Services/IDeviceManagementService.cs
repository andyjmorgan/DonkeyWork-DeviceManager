namespace DonkeyWork.DeviceManager.Api.Services;

using DonkeyWork.DeviceManager.Common.Models;

/// <summary>
/// Service for managing devices.
/// </summary>
public interface IDeviceManagementService
{
    /// <summary>
    /// Gets a paginated list of devices for the current tenant with their room and building information.
    /// </summary>
    Task<PaginatedResponse<DeviceResponse>> GetDevicesAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a device from both the database and Keycloak.
    /// </summary>
    Task DeleteDeviceAsync(Guid deviceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a device's description.
    /// </summary>
    Task UpdateDeviceDescriptionAsync(Guid deviceId, string? description, CancellationToken cancellationToken = default);
}
