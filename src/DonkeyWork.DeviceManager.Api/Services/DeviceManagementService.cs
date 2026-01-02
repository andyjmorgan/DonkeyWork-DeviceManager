namespace DonkeyWork.DeviceManager.Api.Services;

using System.Net.Http.Json;
using DonkeyWork.DeviceManager.Backend.Common.RequestContextServices;
using DonkeyWork.DeviceManager.Common.Models;
using DonkeyWork.DeviceManager.Api.Configuration;
using DonkeyWork.DeviceManager.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Service for managing devices.
/// </summary>
public class DeviceManagementService : IDeviceManagementService
{
    private readonly DeviceManagerContext _dbContext;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly KeycloakConfiguration _keycloakConfig;
    private readonly IRequestContextProvider _requestContextProvider;
    private readonly ILogger<DeviceManagementService> _logger;

    public DeviceManagementService(
        DeviceManagerContext dbContext,
        IHttpClientFactory httpClientFactory,
        IOptions<KeycloakConfiguration> keycloakConfig,
        IRequestContextProvider requestContextProvider,
        ILogger<DeviceManagementService> logger)
    {
        _dbContext = dbContext;
        _httpClientFactory = httpClientFactory;
        _keycloakConfig = keycloakConfig.Value;
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

        // Get admin access token
        var adminAccessToken = await GetAdminAccessTokenAsync(cancellationToken);

        // Delete device user from Keycloak (device Id is the Keycloak user ID)
        await DeleteDeviceUserFromKeycloakAsync(adminAccessToken, device.Id.ToString(), cancellationToken);

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

    private async Task<string> GetAdminAccessTokenAsync(CancellationToken cancellationToken)
    {
        var httpClient = _httpClientFactory.CreateClient();
        var tokenUrl = $"{_keycloakConfig.Authority}/protocol/openid-connect/token";

        _logger.LogInformation("Requesting admin access token from: {TokenUrl}", tokenUrl);

        var tokenRequest = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "client_credentials"),
            new KeyValuePair<string, string>("client_id", _keycloakConfig.AdminClientId),
            new KeyValuePair<string, string>("client_secret", _keycloakConfig.AdminClientSecret)
        });

        var response = await httpClient.PostAsync(tokenUrl, tokenRequest, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Failed to get admin token. Status: {StatusCode}, Body: {ErrorBody}",
                response.StatusCode, errorBody);
            throw new InvalidOperationException($"Failed to get admin access token: {response.StatusCode} - {errorBody}");
        }

        var tokenResponse = await response.Content.ReadFromJsonAsync<KeycloakTokenResponse>(cancellationToken);
        if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Failed to deserialize admin token response. Body: {Body}", body);
            throw new InvalidOperationException("Failed to get admin access token from Keycloak");
        }

        _logger.LogInformation("Successfully obtained admin access token");
        return tokenResponse.AccessToken;
    }

    private async Task DeleteDeviceUserFromKeycloakAsync(
        string adminAccessToken,
        string keycloakUserId,
        CancellationToken cancellationToken)
    {
        var httpClient = _httpClientFactory.CreateClient();
        var realm = _keycloakConfig.Authority.Split('/').Last();
        var baseUrl = _keycloakConfig.Authority.Replace($"/realms/{realm}", "");
        var deleteUserUrl = $"{baseUrl}/admin/realms/{realm}/users/{keycloakUserId}";

        _logger.LogInformation("Deleting device user from Keycloak: {KeycloakUserId}", keycloakUserId);

        var request = new HttpRequestMessage(HttpMethod.Delete, deleteUserUrl)
        {
            Headers = { { "Authorization", $"Bearer {adminAccessToken}" } }
        };

        var response = await httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Failed to delete device user from Keycloak. Status: {StatusCode}, Body: {ErrorBody}",
                response.StatusCode, errorBody);
            throw new InvalidOperationException($"Failed to delete device user from Keycloak: {response.StatusCode} - {errorBody}");
        }

        _logger.LogInformation("Successfully deleted device user from Keycloak: {KeycloakUserId}", keycloakUserId);
    }

    private record KeycloakTokenResponse(
        [property: System.Text.Json.Serialization.JsonPropertyName("access_token")] string AccessToken,
        [property: System.Text.Json.Serialization.JsonPropertyName("token_type")] string TokenType,
        [property: System.Text.Json.Serialization.JsonPropertyName("expires_in")] int ExpiresIn,
        [property: System.Text.Json.Serialization.JsonPropertyName("refresh_token")] string? RefreshToken,
        [property: System.Text.Json.Serialization.JsonPropertyName("id_token")] string? IdToken);
}
