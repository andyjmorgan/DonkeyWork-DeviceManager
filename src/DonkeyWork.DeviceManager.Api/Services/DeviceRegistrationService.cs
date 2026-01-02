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
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly KeycloakConfiguration _keycloakConfig;
    private readonly IHubContext<DeviceRegistrationHub> _hubContext;
    private readonly IRequestContextProvider _requestContextProvider;
    private readonly ILogger<DeviceRegistrationService> _logger;

    public DeviceRegistrationService(
        DeviceManagerContext dbContext,
        IHttpClientFactory httpClientFactory,
        IOptions<KeycloakConfiguration> keycloakConfig,
        IHubContext<DeviceRegistrationHub> hubContext,
        IRequestContextProvider requestContextProvider,
        ILogger<DeviceRegistrationService> logger)
    {
        _dbContext = dbContext;
        _httpClientFactory = httpClientFactory;
        _keycloakConfig = keycloakConfig.Value;
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

        // Get admin access token
        var adminAccessToken = await GetAdminAccessTokenAsync(cancellationToken);

        // Create Keycloak user for device - Keycloak will assign the actual ID
        var keycloakUserId = await CreateDeviceUserInKeycloakAsync(
            adminAccessToken,
            deviceUsername,
            deviceEmail,
            tenantId,
            cancellationToken);

        _logger.LogInformation("Created Keycloak device user: {DeviceUsername}, KeycloakUserId: {KeycloakUserId}, TenantId: {TenantId}",
            deviceUsername, keycloakUserId, tenantId);

        // Set a temporary password for the device
        var devicePassword = Guid.NewGuid().ToString();
        await SetDevicePasswordAsync(adminAccessToken, keycloakUserId, devicePassword, cancellationToken);

        // Clear any required actions that might have been added
        await ClearRequiredActionsAsync(adminAccessToken, keycloakUserId, cancellationToken);

        // Get tokens for the device
        var tokens = await GetDeviceTokensAsync(deviceUsername, devicePassword, cancellationToken);

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
            AccessToken = tokens.AccessToken,
            RefreshToken = tokens.RefreshToken ?? throw new InvalidOperationException("Refresh token not returned by Keycloak"),
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

    private async Task<string> GetAdminAccessTokenAsync(CancellationToken cancellationToken)
    {
        var httpClient = _httpClientFactory.CreateClient();
        var tokenUrl = $"{_keycloakConfig.Authority}/protocol/openid-connect/token";

        _logger.LogInformation("Requesting admin access token from: {TokenUrl}", tokenUrl);
        _logger.LogInformation("Using AdminClientId: {AdminClientId}", _keycloakConfig.AdminClientId);

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

        _logger.LogInformation("Successfully obtained admin access token (length: {Length} chars)", tokenResponse.AccessToken.Length);
        return tokenResponse.AccessToken;
    }

    private async Task<string> CreateDeviceUserInKeycloakAsync(
        string adminAccessToken,
        string username,
        string email,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var httpClient = _httpClientFactory.CreateClient();
        var realm = _keycloakConfig.Authority.Split('/').Last();
        var baseUrl = _keycloakConfig.Authority.Replace($"/realms/{realm}", "");
        var createUserUrl = $"{baseUrl}/admin/realms/{realm}/users";

        _logger.LogInformation("Creating device user in Keycloak");
        _logger.LogInformation("Authority: {Authority}", _keycloakConfig.Authority);
        _logger.LogInformation("Realm: {Realm}", realm);
        _logger.LogInformation("Base URL: {BaseUrl}", baseUrl);
        _logger.LogInformation("Create User URL: {CreateUserUrl}", createUserUrl);
        _logger.LogInformation("Username: {Username}, Email: {Email}, TenantId: {TenantId}", username, email, tenantId);

        var createUserRequest = new
        {
            username,
            email,
            firstName = "Device",
            lastName = username,
            enabled = true,
            emailVerified = true,
            requiredActions = Array.Empty<string>(),
            attributes = new Dictionary<string, string[]>
            {
                { "tenantId", new[] { tenantId.ToString() } },
                { "isDevice", new[] { "true" } }
            }
        };

        var serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        var requestBody = JsonSerializer.Serialize(createUserRequest, serializerOptions);
        _logger.LogInformation("Request body: {RequestBody}", requestBody);

        // Decode and log token claims for debugging
        var handler = new JwtSecurityTokenHandler();
        var jsonToken = handler.ReadToken(adminAccessToken) as JwtSecurityToken;
        if (jsonToken != null)
        {
            var subject = jsonToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
            var clientId = jsonToken.Claims.FirstOrDefault(c => c.Type == "azp" || c.Type == "client_id")?.Value;
            var audience = string.Join(", ", jsonToken.Audiences);
            var resourceAccess = jsonToken.Claims.FirstOrDefault(c => c.Type == "resource_access")?.Value;

            _logger.LogInformation("Token details - Subject: {Subject}, ClientId: {ClientId}, Audience: {Audience}",
                subject, clientId, audience);
            _logger.LogInformation("Token resource_access claim: {ResourceAccess}", resourceAccess);
        }
        _logger.LogInformation("Token length: {TokenLength} chars, starts with: {TokenStart}",
            adminAccessToken.Length, adminAccessToken.Substring(0, Math.Min(20, adminAccessToken.Length)));

        var request = new HttpRequestMessage(HttpMethod.Post, createUserUrl)
        {
            Headers = { { "Authorization", $"Bearer {adminAccessToken}" } },
            Content = new StringContent(requestBody, Encoding.UTF8, "application/json")
        };

        var response = await httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to create device user. Status: {StatusCode}, Body: {Body}",
                response.StatusCode, body);
            throw new InvalidOperationException($"Failed to create device user in Keycloak: {response.StatusCode} - {body}");
        }

        _logger.LogInformation("Device user created successfully. Response: {Body}", body);

        var locationHeader = response.Headers.Location?.ToString();
        if (string.IsNullOrEmpty(locationHeader))
        {
            throw new InvalidOperationException("Failed to get device user ID from Keycloak response");
        }

        var userId = locationHeader.Split('/').Last();
        return userId;
    }

    private async Task SetDevicePasswordAsync(
        string adminAccessToken,
        string keycloakUserId,
        string password,
        CancellationToken cancellationToken)
    {
        var httpClient = _httpClientFactory.CreateClient();
        var realm = _keycloakConfig.Authority.Split('/').Last();
        var resetPasswordUrl = $"{_keycloakConfig.Authority.Replace($"/realms/{realm}", "")}/admin/realms/{realm}/users/{keycloakUserId}/reset-password";

        var resetPasswordRequest = new
        {
            type = "password",
            value = password,
            temporary = false
        };

        var request = new HttpRequestMessage(HttpMethod.Put, resetPasswordUrl)
        {
            Headers = { { "Authorization", $"Bearer {adminAccessToken}" } },
            Content = new StringContent(JsonSerializer.Serialize(resetPasswordRequest), Encoding.UTF8, "application/json")
        };

        var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private async Task ClearRequiredActionsAsync(
        string adminAccessToken,
        string keycloakUserId,
        CancellationToken cancellationToken)
    {
        var httpClient = _httpClientFactory.CreateClient();
        var realm = _keycloakConfig.Authority.Split('/').Last();
        var baseUrl = _keycloakConfig.Authority.Replace($"/realms/{realm}", "");
        var updateUserUrl = $"{baseUrl}/admin/realms/{realm}/users/{keycloakUserId}";

        _logger.LogInformation("Clearing required actions for user {KeycloakUserId}", keycloakUserId);

        var serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        var updateRequest = new
        {
            requiredActions = Array.Empty<string>()
        };

        var request = new HttpRequestMessage(HttpMethod.Put, updateUserUrl)
        {
            Headers = { { "Authorization", $"Bearer {adminAccessToken}" } },
            Content = new StringContent(JsonSerializer.Serialize(updateRequest, serializerOptions), Encoding.UTF8, "application/json")
        };

        var response = await httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Failed to clear required actions. Status: {StatusCode}, Body: {ErrorBody}",
                response.StatusCode, errorBody);
            throw new InvalidOperationException($"Failed to clear required actions in Keycloak: {response.StatusCode} - {errorBody}");
        }

        _logger.LogInformation("Successfully cleared required actions for user {KeycloakUserId}", keycloakUserId);
    }

    private async Task<KeycloakTokenResponse> GetDeviceTokensAsync(
        string username,
        string password,
        CancellationToken cancellationToken)
    {
        var httpClient = _httpClientFactory.CreateClient();
        var tokenUrl = $"{_keycloakConfig.Authority}/protocol/openid-connect/token";

        var tokenRequest = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "password"),
            new KeyValuePair<string, string>("client_id", _keycloakConfig.ClientId),
            new KeyValuePair<string, string>("client_secret", _keycloakConfig.ClientSecret),
            new KeyValuePair<string, string>("username", username),
            new KeyValuePair<string, string>("password", password)
        });

        var response = await httpClient.PostAsync(tokenUrl, tokenRequest, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Failed to get device tokens. Status: {StatusCode}, Body: {ErrorBody}",
                response.StatusCode, errorBody);
            throw new InvalidOperationException($"Failed to get device tokens from Keycloak: {response.StatusCode} - {errorBody}");
        }

        var tokenResponse = await response.Content.ReadFromJsonAsync<KeycloakTokenResponse>(cancellationToken);
        if (tokenResponse == null)
        {
            throw new InvalidOperationException("Failed to get device tokens from Keycloak");
        }

        return tokenResponse;
    }

    private record KeycloakTokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("token_type")] string TokenType,
        [property: JsonPropertyName("expires_in")] int ExpiresIn,
        [property: JsonPropertyName("refresh_token")] string? RefreshToken,
        [property: JsonPropertyName("id_token")] string? IdToken);
}
