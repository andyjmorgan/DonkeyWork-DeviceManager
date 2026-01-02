using DonkeyWork.DeviceManager.Backend.Common.RequestContextServices;
using DonkeyWork.DeviceManager.Common.Models.Building;
using DonkeyWork.DeviceManager.Common.Models.Provisioning;
using DonkeyWork.DeviceManager.Common.Models.Room;
using DonkeyWork.DeviceManager.Persistence.Context;
using DonkeyWork.DeviceManager.Persistence.Entity;
using Microsoft.EntityFrameworkCore;

namespace DonkeyWork.DeviceManager.Api.Services;

/// <summary>
/// Service for managing organizational structure (buildings and rooms).
/// </summary>
public class OrganizationService : IOrganizationService
{
    private readonly DeviceManagerContext _dbContext;
    private readonly IRequestContextProvider _requestContextProvider;
    private readonly ILogger<OrganizationService> _logger;

    public OrganizationService(
        DeviceManagerContext dbContext,
        IRequestContextProvider requestContextProvider,
        ILogger<OrganizationService> logger)
    {
        _dbContext = dbContext;
        _requestContextProvider = requestContextProvider;
        _logger = logger;
    }

    // Building operations

    public async Task<List<BuildingResponse>> GetBuildingsAsync()
    {
        var tenantId = _requestContextProvider.Context.TenantId;
        _logger.LogInformation("Getting buildings for tenant {TenantId}", tenantId);

        var buildings = await _dbContext.Buildings
            .Include(b => b.Rooms)
            .OrderBy(b => b.Name)
            .Select(b => new BuildingResponse
            {
                Id = b.Id,
                Name = b.Name,
                Description = b.Description,
                RoomCount = b.Rooms.Count,
                Created = b.Created,
                Updated = b.Updated
            })
            .ToListAsync();

        _logger.LogInformation("Found {BuildingCount} buildings for tenant {TenantId}", buildings.Count, tenantId);
        return buildings;
    }

    public async Task<BuildingDetailsResponse?> GetBuildingByIdAsync(Guid id)
    {
        var tenantId = _requestContextProvider.Context.TenantId;
        _logger.LogInformation("Getting building {BuildingId} for tenant {TenantId}", id, tenantId);

        var building = await _dbContext.Buildings
            .Include(b => b.Rooms)
            .Where(b => b.Id == id)
            .Select(b => new BuildingDetailsResponse
            {
                Id = b.Id,
                Name = b.Name,
                Description = b.Description,
                Rooms = b.Rooms.Select(r => new RoomResponse
                {
                    Id = r.Id,
                    Name = r.Name,
                    Description = r.Description,
                    BuildingId = b.Id,
                    BuildingName = b.Name,
                    DeviceCount = r.Devices.Count,
                    Created = r.Created,
                    Updated = r.Updated
                }).ToList(),
                Created = b.Created,
                Updated = b.Updated
            })
            .FirstOrDefaultAsync();

        if (building == null)
        {
            _logger.LogWarning("Building {BuildingId} not found for tenant {TenantId}", id, tenantId);
        }

        return building;
    }

    public async Task<BuildingResponse> CreateBuildingAsync(CreateBuildingRequest request)
    {
        var tenantId = _requestContextProvider.Context.TenantId;
        _logger.LogInformation("Creating building '{BuildingName}' for tenant {TenantId}", request.Name, tenantId);

        var building = new BuildingEntity
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description
        };

        _dbContext.Buildings.Add(building);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Created building {BuildingId} for tenant {TenantId}", building.Id, tenantId);

        return new BuildingResponse
        {
            Id = building.Id,
            Name = building.Name,
            Description = building.Description,
            RoomCount = 0,
            Created = building.Created,
            Updated = building.Updated
        };
    }

    public async Task<BuildingResponse?> UpdateBuildingAsync(Guid id, UpdateBuildingRequest request)
    {
        var tenantId = _requestContextProvider.Context.TenantId;
        _logger.LogInformation("Updating building {BuildingId} for tenant {TenantId}", id, tenantId);

        var building = await _dbContext.Buildings
            .Include(b => b.Rooms)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (building == null)
        {
            _logger.LogWarning("Building {BuildingId} not found for tenant {TenantId}", id, tenantId);
            return null;
        }

        building.Name = request.Name;
        building.Description = request.Description;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Updated building {BuildingId} for tenant {TenantId}", id, tenantId);

        return new BuildingResponse
        {
            Id = building.Id,
            Name = building.Name,
            Description = building.Description,
            RoomCount = building.Rooms.Count,
            Created = building.Created,
            Updated = building.Updated
        };
    }

    public async Task<bool> DeleteBuildingAsync(Guid id)
    {
        var tenantId = _requestContextProvider.Context.TenantId;
        _logger.LogInformation("Deleting building {BuildingId} for tenant {TenantId}", id, tenantId);

        var building = await _dbContext.Buildings
            .Include(b => b.Rooms)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (building == null)
        {
            _logger.LogWarning("Building {BuildingId} not found for tenant {TenantId}", id, tenantId);
            return false;
        }

        if (building.Rooms.Any())
        {
            _logger.LogWarning("Cannot delete building {BuildingId} - has {RoomCount} rooms", id, building.Rooms.Count);
            return false;
        }

        _dbContext.Buildings.Remove(building);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Deleted building {BuildingId} for tenant {TenantId}", id, tenantId);
        return true;
    }

    // Room operations

    public async Task<List<RoomResponse>> GetRoomsAsync(Guid? buildingId = null)
    {
        var tenantId = _requestContextProvider.Context.TenantId;
        _logger.LogInformation("Getting rooms for tenant {TenantId}, buildingId filter: {BuildingId}", tenantId, buildingId);

        var query = _dbContext.Rooms
            .Include(r => r.Building)
            .Include(r => r.Devices)
            .AsQueryable();

        if (buildingId.HasValue)
        {
            query = query.Where(r => r.Building.Id == buildingId.Value);
        }

        var rooms = await query
            .OrderBy(r => r.Building.Name)
            .ThenBy(r => r.Name)
            .Select(r => new RoomResponse
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                BuildingId = r.Building.Id,
                BuildingName = r.Building.Name,
                DeviceCount = r.Devices.Count,
                Created = r.Created,
                Updated = r.Updated
            })
            .ToListAsync();

        _logger.LogInformation("Found {RoomCount} rooms for tenant {TenantId}", rooms.Count, tenantId);
        return rooms;
    }

    public async Task<RoomDetailsResponse?> GetRoomByIdAsync(Guid id)
    {
        var tenantId = _requestContextProvider.Context.TenantId;
        _logger.LogInformation("Getting room {RoomId} for tenant {TenantId}", id, tenantId);

        var room = await _dbContext.Rooms
            .Include(r => r.Building)
            .Include(r => r.Devices)
            .Where(r => r.Id == id)
            .Select(r => new RoomDetailsResponse
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                Building = new BuildingResponse
                {
                    Id = r.Building.Id,
                    Name = r.Building.Name,
                    Description = r.Building.Description,
                    RoomCount = r.Building.Rooms.Count,
                    Created = r.Building.Created,
                    Updated = r.Building.Updated
                },
                Devices = r.Devices.Select(d => new Common.Models.DeviceResponse
                {
                    Id = d.Id,
                    Name = d.Name,
                    Description = d.Description,
                    Online = d.Online,
                    CreatedAt = d.CreatedAt,
                    LastSeen = d.LastSeen,
                    Room = new Common.Models.DeviceRoomResponse
                    {
                        Id = r.Id,
                        Name = r.Name,
                        Building = new Common.Models.DeviceBuildingResponse
                        {
                            Id = r.Building.Id,
                            Name = r.Building.Name
                        }
                    },
                    CpuCores = d.CpuCores,
                    TotalMemoryBytes = d.TotalMemoryBytes,
                    OperatingSystem = d.OperatingSystem,
                    OSArchitecture = d.OSArchitecture,
                    Architecture = d.Architecture,
                    OperatingSystemVersion = d.OperatingSystemVersion
                }).ToList(),
                Created = r.Created,
                Updated = r.Updated
            })
            .FirstOrDefaultAsync();

        if (room == null)
        {
            _logger.LogWarning("Room {RoomId} not found for tenant {TenantId}", id, tenantId);
        }

        return room;
    }

    public async Task<RoomResponse> CreateRoomAsync(CreateRoomRequest request)
    {
        var tenantId = _requestContextProvider.Context.TenantId;
        _logger.LogInformation("Creating room '{RoomName}' in building {BuildingId} for tenant {TenantId}",
            request.Name, request.BuildingId, tenantId);

        // Verify building exists
        var building = await _dbContext.Buildings.FindAsync(request.BuildingId);
        if (building == null)
        {
            _logger.LogWarning("Building {BuildingId} not found for tenant {TenantId}", request.BuildingId, tenantId);
            throw new InvalidOperationException($"Building {request.BuildingId} not found");
        }

        var room = new RoomEntity
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            Building = building
        };

        _dbContext.Rooms.Add(room);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Created room {RoomId} in building {BuildingId} for tenant {TenantId}",
            room.Id, request.BuildingId, tenantId);

        return new RoomResponse
        {
            Id = room.Id,
            Name = room.Name,
            Description = room.Description,
            BuildingId = building.Id,
            BuildingName = building.Name,
            DeviceCount = 0,
            Created = room.Created,
            Updated = room.Updated
        };
    }

    public async Task<RoomResponse?> UpdateRoomAsync(Guid id, UpdateRoomRequest request)
    {
        var tenantId = _requestContextProvider.Context.TenantId;
        _logger.LogInformation("Updating room {RoomId} for tenant {TenantId}", id, tenantId);

        var room = await _dbContext.Rooms
            .Include(r => r.Building)
            .Include(r => r.Devices)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (room == null)
        {
            _logger.LogWarning("Room {RoomId} not found for tenant {TenantId}", id, tenantId);
            return null;
        }

        // If building is changing, verify new building exists
        if (room.Building.Id != request.BuildingId)
        {
            var newBuilding = await _dbContext.Buildings.FindAsync(request.BuildingId);
            if (newBuilding == null)
            {
                _logger.LogWarning("Building {BuildingId} not found for tenant {TenantId}", request.BuildingId, tenantId);
                throw new InvalidOperationException($"Building {request.BuildingId} not found");
            }
            room.Building = newBuilding;
        }

        room.Name = request.Name;
        room.Description = request.Description;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Updated room {RoomId} for tenant {TenantId}", id, tenantId);

        return new RoomResponse
        {
            Id = room.Id,
            Name = room.Name,
            Description = room.Description,
            BuildingId = room.Building.Id,
            BuildingName = room.Building.Name,
            DeviceCount = room.Devices.Count,
            Created = room.Created,
            Updated = room.Updated
        };
    }

    public async Task<bool> DeleteRoomAsync(Guid id)
    {
        var tenantId = _requestContextProvider.Context.TenantId;
        _logger.LogInformation("Deleting room {RoomId} for tenant {TenantId}", id, tenantId);

        var room = await _dbContext.Rooms
            .Include(r => r.Devices)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (room == null)
        {
            _logger.LogWarning("Room {RoomId} not found for tenant {TenantId}", id, tenantId);
            return false;
        }

        if (room.Devices.Any())
        {
            _logger.LogWarning("Cannot delete room {RoomId} - has {DeviceCount} devices", id, room.Devices.Count);
            return false;
        }

        _dbContext.Rooms.Remove(room);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Deleted room {RoomId} for tenant {TenantId}", id, tenantId);
        return true;
    }

    // Provisioning operations

    public async Task<ProvisionOrganizationResponse> EnsureOrganizationStructureAsync()
    {
        var tenantId = _requestContextProvider.Context.TenantId;
        _logger.LogInformation("Ensuring organization structure for tenant {TenantId}", tenantId);

        // Check if any buildings exist
        var existingBuilding = await _dbContext.Buildings
            .Include(b => b.Rooms)
            .FirstOrDefaultAsync();

        if (existingBuilding != null)
        {
            _logger.LogInformation("Organization structure already exists for tenant {TenantId}", tenantId);

            var existingRoom = existingBuilding.Rooms.FirstOrDefault();
            if (existingRoom == null)
            {
                // Building exists but no rooms - create default room
                _logger.LogInformation("Creating default room for existing building {BuildingId}", existingBuilding.Id);
                existingRoom = new RoomEntity
                {
                    Id = Guid.NewGuid(),
                    Name = "Default Room",
                    Description = "Automatically created default room",
                    Building = existingBuilding
                };
                _dbContext.Rooms.Add(existingRoom);
                await _dbContext.SaveChangesAsync();
            }

            return new ProvisionOrganizationResponse
            {
                Building = new BuildingResponse
                {
                    Id = existingBuilding.Id,
                    Name = existingBuilding.Name,
                    Description = existingBuilding.Description,
                    RoomCount = existingBuilding.Rooms.Count,
                    Created = existingBuilding.Created,
                    Updated = existingBuilding.Updated
                },
                Room = new RoomResponse
                {
                    Id = existingRoom.Id,
                    Name = existingRoom.Name,
                    Description = existingRoom.Description,
                    BuildingId = existingBuilding.Id,
                    BuildingName = existingBuilding.Name,
                    DeviceCount = existingRoom.Devices?.Count ?? 0,
                    Created = existingRoom.Created,
                    Updated = existingRoom.Updated
                },
                Created = false
            };
        }

        // Create default building and room
        _logger.LogInformation("Creating default organization structure for tenant {TenantId}", tenantId);

        var building = new BuildingEntity
        {
            Id = Guid.NewGuid(),
            Name = "Default Building",
            Description = "Automatically created default building"
        };

        _dbContext.Buildings.Add(building);
        await _dbContext.SaveChangesAsync();

        var room = new RoomEntity
        {
            Id = Guid.NewGuid(),
            Name = "Default Room",
            Description = "Automatically created default room",
            Building = building
        };

        _dbContext.Rooms.Add(room);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Created default organization structure for tenant {TenantId}: building {BuildingId}, room {RoomId}",
            tenantId, building.Id, room.Id);

        return new ProvisionOrganizationResponse
        {
            Building = new BuildingResponse
            {
                Id = building.Id,
                Name = building.Name,
                Description = building.Description,
                RoomCount = 1,
                Created = building.Created,
                Updated = building.Updated
            },
            Room = new RoomResponse
            {
                Id = room.Id,
                Name = room.Name,
                Description = room.Description,
                BuildingId = building.Id,
                BuildingName = building.Name,
                DeviceCount = 0,
                Created = room.Created,
                Updated = room.Updated
            },
            Created = true
        };
    }
}
