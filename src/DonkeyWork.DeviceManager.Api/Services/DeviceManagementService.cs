namespace DonkeyWork.DeviceManager.Api.Services;

using DonkeyWork.DeviceManager.Backend.Common.RequestContextServices;
using DonkeyWork.DeviceManager.Common.Models;
using DonkeyWork.DeviceManager.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

/// <summary>
/// Service for managing devices.
/// </summary>
public class DeviceManagementService : IDeviceManagementService
{
    private readonly DeviceManagerContext _dbContext;
    private readonly IKeycloakService _keycloakService;
    private readonly IRequestContextProvider _requestContextProvider;
    private readonly ILogger<DeviceManagementService> _logger;

    public DeviceManagementService(
        DeviceManagerContext dbContext,
        IKeycloakService keycloakService,
        IRequestContextProvider requestContextProvider,
        ILogger<DeviceManagementService> logger)
    {
        _dbContext = dbContext;
        _keycloakService = keycloakService;
        _requestContextProvider = requestContextProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<PaginatedResponse<DeviceResponse>> GetDevicesAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var tenantId = _requestContextProvider.Context.TenantId;

        // Validate pagination parameters
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        _logger.LogInformation("Getting devices for tenant {TenantId} - Page: {Page}, PageSize: {PageSize}",
            tenantId, page, pageSize);

        // Get total count
        var totalCount = await _dbContext.Devices.CountAsync(cancellationToken);

        // Get paginated devices
        var devices = await _dbContext.Devices
            .Include(d => d.Room)
                .ThenInclude(r => r.Building)
            .OrderBy(d => d.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Found {DeviceCount} devices (page {Page} of {TotalPages}) for tenant {TenantId}",
            devices.Count, page, (int)Math.Ceiling((double)totalCount / pageSize), tenantId);

        var deviceResponses = devices.Select(d => new DeviceResponse
        {
            Id = d.Id,
            Name = d.Name,
            Description = string.IsNullOrEmpty(d.Description) ? null : d.Description,
            Online = d.Online,
            CreatedAt = d.CreatedAt,
            LastSeen = d.LastSeen,
            Room = new DeviceRoomResponse
            {
                Id = d.Room.Id,
                Name = d.Room.Name,
                Building = new DeviceBuildingResponse
                {
                    Id = d.Room.Building.Id,
                    Name = d.Room.Building.Name
                }
            },
            CpuCores = d.CpuCores,
            TotalMemoryBytes = d.TotalMemoryBytes,
            OperatingSystem = d.OperatingSystem,
            OSArchitecture = d.OSArchitecture,
            Architecture = d.Architecture,
            OperatingSystemVersion = d.OperatingSystemVersion
        }).ToList();

        return new PaginatedResponse<DeviceResponse>
        {
            Items = deviceResponses,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    /// <inheritdoc />
    public async Task DeleteDeviceAsync(Guid deviceId, CancellationToken cancellationToken = default)
    {
        var tenantId = _requestContextProvider.Context.TenantId;

        _logger.LogInformation("Deleting device {DeviceId} for tenant {TenantId}", deviceId, tenantId);

        // Find the device
        var device = await _dbContext.Devices
            .FirstOrDefaultAsync(d => d.Id == deviceId, cancellationToken);

        if (device == null)
        {
            throw new InvalidOperationException($"Device with ID '{deviceId}' not found or does not belong to tenant '{tenantId}'.");
        }

        // Delete device user from Keycloak (device Id is the Keycloak user ID)
        await _keycloakService.DeleteUserAsync(device.Id.ToString(), cancellationToken);

        // Delete device from database
        _dbContext.Devices.Remove(device);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully deleted device {DeviceId} for tenant {TenantId}",
            deviceId, tenantId);
    }

    /// <inheritdoc />
    public async Task UpdateDeviceDescriptionAsync(Guid deviceId, string? description, CancellationToken cancellationToken = default)
    {
        var tenantId = _requestContextProvider.Context.TenantId;

        _logger.LogInformation("Updating description for device {DeviceId} in tenant {TenantId}", deviceId, tenantId);

        // Find the device
        var device = await _dbContext.Devices
            .FirstOrDefaultAsync(d => d.Id == deviceId, cancellationToken);

        if (device == null)
        {
            throw new InvalidOperationException($"Device with ID '{deviceId}' not found or does not belong to tenant '{tenantId}'.");
        }

        // Update the description
        device.Description = description ?? string.Empty;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully updated description for device {DeviceId} in tenant {TenantId}",
            deviceId, tenantId);
    }
}
