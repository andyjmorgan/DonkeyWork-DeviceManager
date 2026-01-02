namespace DonkeyWork.DeviceManager.Api.Services;

using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DonkeyWork.DeviceManager.Backend.Common.RequestContextServices;
using DonkeyWork.DeviceManager.Common.Models;
using DonkeyWork.DeviceManager.Common.Models.Registration;
using DonkeyWork.DeviceManager.Api.Hubs;
using DonkeyWork.DeviceManager.Api.Configuration;
using DonkeyWork.DeviceManager.Persistence.Context;
using DonkeyWork.DeviceManager.Persistence.Entity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Service for handling device registration completion.
/// </summary>
public class DeviceRegistrationService : IDeviceRegistrationService
{
    private readonly DeviceManagerContext _dbContext;
    private readonly IKeycloakService _keycloakService;
    private readonly IHubContext<DeviceRegistrationHub> _hubContext;
    private readonly IRequestContextProvider _requestContextProvider;
    private readonly ILogger<DeviceRegistrationService> _logger;

    public DeviceRegistrationService(
        DeviceManagerContext dbContext,
        IKeycloakService keycloakService,
        IHubContext<DeviceRegistrationHub> hubContext,
        IRequestContextProvider requestContextProvider,
        ILogger<DeviceRegistrationService> logger)
    {
        _dbContext = dbContext;
        _keycloakService = keycloakService;
        _hubContext = hubContext;
        _requestContextProvider = requestContextProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<DeviceRegistrationLookupResponse> LookupRegistrationAsync(string threeWordCode, CancellationToken cancellationToken = default)
    {
        var userId = _requestContextProvider.Context.UserId;
        var tenantId = _requestContextProvider.Context.TenantId;

        _logger.LogInformation("Looking up device registration - Code: {ThreeWordCode}, UserId: {UserId}, TenantId: {TenantId}",
            threeWordCode, userId, tenantId);

        // Find the registration
        var registration = await _dbContext.DeviceRegistrations
            .FirstOrDefaultAsync(r => r.ThreeWordRegistration == threeWordCode, cancellationToken);

        if (registration == null)
        {
            throw new InvalidOperationException($"Registration with code '{threeWordCode}' not found or has expired.");
        }

        // Debug: Check total buildings in database
        var totalBuildings = await _dbContext.Buildings.CountAsync(cancellationToken);
        _logger.LogInformation("Total buildings in database: {TotalBuildings}", totalBuildings);

        // Get user's buildings and rooms
        var buildings = await _dbContext.Buildings
            .Include(b => b.Rooms)
            .OrderBy(b => b.Name)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Found {BuildingCount} buildings for tenant {TenantId}. Total buildings in DB: {TotalBuildings}",
            buildings.Count, tenantId, totalBuildings);

        // Debug: Log all buildings if none found for this tenant
        if (buildings.Count == 0)
        {
            var allBuildings = await _dbContext.Buildings.Select(b => new { b.Id, b.TenantId, b.Name }).ToListAsync(cancellationToken);
            foreach (var b in allBuildings)
            {
                _logger.LogInformation("Building in DB: Id={BuildingId}, TenantId={TenantId}, Name={Name}", b.Id, b.TenantId, b.Name);
            }
        }

        var buildingDtos = buildings.Select(b => new BuildingResponse
        {
            Id = b.Id,
            Name = b.Name,
            Rooms = b.Rooms.Select(r => new RoomResponse
            {
                Id = r.Id,
                Name = r.Name
            }).OrderBy(r => r.Name).ToList()
        }).ToList();

        _logger.LogInformation("Device registration lookup successful - Code: {ThreeWordCode}, UserId: {UserId}, Buildings: {BuildingCount}",
            threeWordCode, userId, buildings.Count);

        return new DeviceRegistrationLookupResponse
        {
            RegistrationId = registration.Id,
            ThreeWordCode = threeWordCode,
            Buildings = buildingDtos
        };
    }

    /// <inheritdoc />
    public async Task<DeviceCredentialsResponse> CompleteRegistrationAsync(string threeWordCode, Guid roomId, CancellationToken cancellationToken = default)
    {
        var userId = _requestContextProvider.Context.UserId;
        var tenantId = _requestContextProvider.Context.TenantId;

        // Find the registration
        var registration = await _dbContext.DeviceRegistrations
            .FirstOrDefaultAsync(r => r.ThreeWordRegistration == threeWordCode, cancellationToken);

        if (registration == null)
        {
            throw new InvalidOperationException($"Registration with code '{threeWordCode}' not found or has expired.");
        }

        // Generate a temporary identifier for the device username/email
        var tempDeviceId = Guid.NewGuid();
        var deviceUsername = $"device-{tempDeviceId}";
        var deviceEmail = $"device-{tempDeviceId}@devices.local";

        // Create Keycloak user for device - Keycloak will assign the actual ID
        var keycloakUserId = await _keycloakService.CreateDeviceUserAsync(
            deviceUsername,
            deviceEmail,
            tenantId,
            cancellationToken);

        _logger.LogInformation("Created Keycloak device user: {DeviceUsername}, KeycloakUserId: {KeycloakUserId}, TenantId: {TenantId}",
            deviceUsername, keycloakUserId, tenantId);

        // Set a temporary password for the device
        var devicePassword = Guid.NewGuid().ToString();
        await _keycloakService.SetUserPasswordAsync(keycloakUserId, devicePassword, false, cancellationToken);

        // Clear any required actions that might have been added
        await _keycloakService.ClearRequiredActionsAsync(keycloakUserId, cancellationToken);

        // Get tokens for the device
        var (accessToken, refreshToken, expiresIn) = await _keycloakService.GetUserTokensAsync(deviceUsername, devicePassword, cancellationToken);

        // Verify room exists and belongs to the tenant
        var room = await _dbContext.Rooms
            .Include(r => r.Building)
            .FirstOrDefaultAsync(r => r.Id == roomId && r.Building.TenantId == tenantId, cancellationToken);

        if (room == null)
        {
            throw new InvalidOperationException($"Room with ID '{roomId}' not found or does not belong to tenant '{tenantId}'.");
        }

        // Create Device entity in database
        var now = DateTimeOffset.UtcNow;
        var deviceId = Guid.Parse(keycloakUserId);
        var device = new DeviceEntity
        {
            Id = deviceId,
            Name = $"Device {keycloakUserId}",
            Room = room,
            TenantId = tenantId,
            UserId = userId,
            CreatedAt = now,
            LastSeen = now
        };

        _dbContext.Devices.Add(device);

        _logger.LogInformation("Created device entity - DeviceId: {DeviceId}, Name: {Name}, RoomId: {RoomId}, BuildingId: {BuildingId}",
            device.Id, device.Name, room.Id, room.Building.Id);

        // Create device credentials DTO
        var credentials = new DeviceCredentialsResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            DeviceUserId = Guid.Parse(keycloakUserId),
            TenantId = tenantId
        };

        // Save device entity and send credentials to device via SignalR
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _hubContext.Clients.Client(registration.ConnectionId).SendAsync("ReceiveCredentials", credentials, cancellationToken);

        _logger.LogInformation("Sent credentials to device via SignalR - ConnectionId: {ConnectionId}, DeviceUserId: {DeviceUserId}",
            registration.ConnectionId, keycloakUserId);

        // Note: Registration will be cleaned up by hub connection lifecycle
        _logger.LogInformation("Completed device registration - Code: {ThreeWordCode}, DeviceUserId: {DeviceUserId}",
            threeWordCode, keycloakUserId);

        return credentials;
    }
}
