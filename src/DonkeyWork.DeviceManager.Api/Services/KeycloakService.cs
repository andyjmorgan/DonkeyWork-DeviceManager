namespace DonkeyWork.DeviceManager.Api.Services;

using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using DonkeyWork.DeviceManager.Api.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Service for interacting with Keycloak admin APIs.
/// Centralizes all Keycloak operations to avoid code duplication.
/// </summary>
public class KeycloakService : IKeycloakService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly KeycloakConfiguration _keycloakConfig;
    private readonly ILogger<KeycloakService> _logger;

    public KeycloakService(
        IHttpClientFactory httpClientFactory,
        IOptions<KeycloakConfiguration> keycloakConfig,
        ILogger<KeycloakService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _keycloakConfig = keycloakConfig.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string> GetAdminAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        var httpClient = _httpClientFactory.CreateClient();
        var tokenUrl = $"{_keycloakConfig.BackchannelAuthority}/protocol/openid-connect/token";

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

    /// <inheritdoc />
    public async Task<string> CreateDeviceUserAsync(string username, string email, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var adminAccessToken = await GetAdminAccessTokenAsync(cancellationToken);
        var httpClient = _httpClientFactory.CreateClient();
        var realm = _keycloakConfig.BackchannelAuthority.Split('/').Last();
        var baseUrl = _keycloakConfig.BackchannelAuthority.Replace($"/realms/{realm}", "");
        var createUserUrl = $"{baseUrl}/admin/realms/{realm}/users";

        _logger.LogInformation("Creating device user in Keycloak: {Username}, Email: {Email}, TenantId: {TenantId}",
            username, email, tenantId);

        var userPayload = new
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

        var request = new HttpRequestMessage(HttpMethod.Post, createUserUrl)
        {
            Content = new StringContent(JsonSerializer.Serialize(userPayload), Encoding.UTF8, "application/json"),
            Headers = { { "Authorization", $"Bearer {adminAccessToken}" } }
        };

        var response = await httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Failed to create device user in Keycloak. Status: {StatusCode}, Body: {ErrorBody}",
                response.StatusCode, errorBody);
            throw new InvalidOperationException($"Failed to create device user in Keycloak: {response.StatusCode} - {errorBody}");
        }

        var locationHeader = response.Headers.Location?.ToString();
        if (string.IsNullOrEmpty(locationHeader))
        {
            throw new InvalidOperationException("Failed to get device user ID from Keycloak response");
        }

        var userId = locationHeader.Split('/').Last();
        _logger.LogInformation("Successfully created device user in Keycloak: {Username}, KeycloakUserId: {UserId}", username, userId);
        return userId;
    }

    /// <inheritdoc />
    public async Task SetUserPasswordAsync(string keycloakUserId, string password, bool temporary = false, CancellationToken cancellationToken = default)
    {
        var adminAccessToken = await GetAdminAccessTokenAsync(cancellationToken);
        var httpClient = _httpClientFactory.CreateClient();
        var realm = _keycloakConfig.BackchannelAuthority.Split('/').Last();
        var baseUrl = _keycloakConfig.BackchannelAuthority.Replace($"/realms/{realm}", "");
        var setPasswordUrl = $"{baseUrl}/admin/realms/{realm}/users/{keycloakUserId}/reset-password";

        _logger.LogInformation("Setting password for Keycloak user: {KeycloakUserId}", keycloakUserId);

        var passwordPayload = new
        {
            type = "password",
            value = password,
            temporary
        };

        var request = new HttpRequestMessage(HttpMethod.Put, setPasswordUrl)
        {
            Content = new StringContent(JsonSerializer.Serialize(passwordPayload), Encoding.UTF8, "application/json"),
            Headers = { { "Authorization", $"Bearer {adminAccessToken}" } }
        };

        var response = await httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Failed to set password for Keycloak user. Status: {StatusCode}, Body: {ErrorBody}",
                response.StatusCode, errorBody);
            throw new InvalidOperationException($"Failed to set password for Keycloak user: {response.StatusCode} - {errorBody}");
        }

        _logger.LogInformation("Successfully set password for Keycloak user: {KeycloakUserId}", keycloakUserId);
    }

    /// <inheritdoc />
    public async Task ClearRequiredActionsAsync(string keycloakUserId, CancellationToken cancellationToken = default)
    {
        var adminAccessToken = await GetAdminAccessTokenAsync(cancellationToken);
        var httpClient = _httpClientFactory.CreateClient();
        var realm = _keycloakConfig.BackchannelAuthority.Split('/').Last();
        var baseUrl = _keycloakConfig.BackchannelAuthority.Replace($"/realms/{realm}", "");
        var clearActionsUrl = $"{baseUrl}/admin/realms/{realm}/users/{keycloakUserId}";

        _logger.LogInformation("Clearing required actions for Keycloak user: {KeycloakUserId}", keycloakUserId);

        var updatePayload = new
        {
            requiredActions = Array.Empty<string>()
        };

        var request = new HttpRequestMessage(HttpMethod.Put, clearActionsUrl)
        {
            Content = new StringContent(JsonSerializer.Serialize(updatePayload), Encoding.UTF8, "application/json"),
            Headers = { { "Authorization", $"Bearer {adminAccessToken}" } }
        };

        var response = await httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Failed to clear required actions for Keycloak user. Status: {StatusCode}, Body: {ErrorBody}",
                response.StatusCode, errorBody);
            throw new InvalidOperationException($"Failed to clear required actions for Keycloak user: {response.StatusCode} - {errorBody}");
        }

        _logger.LogInformation("Successfully cleared required actions for Keycloak user: {KeycloakUserId}", keycloakUserId);
    }

    /// <inheritdoc />
    public async Task<(string AccessToken, string RefreshToken, int ExpiresIn)> GetUserTokensAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        var httpClient = _httpClientFactory.CreateClient();
        var tokenUrl = $"{_keycloakConfig.BackchannelAuthority}/protocol/openid-connect/token";

        _logger.LogInformation("Getting tokens for user: {Username}", username);

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
            _logger.LogError("Failed to get user tokens. Status: {StatusCode}, Body: {ErrorBody}",
                response.StatusCode, errorBody);
            throw new InvalidOperationException($"Failed to get user tokens: {response.StatusCode} - {errorBody}");
        }

        var tokenResponse = await response.Content.ReadFromJsonAsync<KeycloakTokenResponse>(cancellationToken);
        if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken) || string.IsNullOrEmpty(tokenResponse.RefreshToken))
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Failed to deserialize user token response. Body: {Body}", body);
            throw new InvalidOperationException("Failed to get user tokens from Keycloak");
        }

        _logger.LogInformation("Successfully obtained user tokens for: {Username}", username);
        return (tokenResponse.AccessToken, tokenResponse.RefreshToken!, tokenResponse.ExpiresIn);
    }

    /// <inheritdoc />
    public async Task<bool> ValidateUserPasswordAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var tokenUrl = $"{_keycloakConfig.BackchannelAuthority}/protocol/openid-connect/token";

            _logger.LogInformation("Validating password for user: {Username}", username);

            var tokenRequest = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "password"),
                new KeyValuePair<string, string>("client_id", _keycloakConfig.ClientId),
                new KeyValuePair<string, string>("username", username),
                new KeyValuePair<string, string>("password", password)
            });

            var response = await httpClient.PostAsync(tokenUrl, tokenRequest, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Password validation successful for user: {Username}", username);
                return true;
            }

            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Password validation failed for user: {Username}. Status: {StatusCode}, Body: {ErrorBody}",
                username, response.StatusCode, errorBody);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating password for user: {Username}", username);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task DeleteUserAsync(string keycloakUserId, CancellationToken cancellationToken = default)
    {
        var adminAccessToken = await GetAdminAccessTokenAsync(cancellationToken);
        var httpClient = _httpClientFactory.CreateClient();
        var realm = _keycloakConfig.BackchannelAuthority.Split('/').Last();
        var baseUrl = _keycloakConfig.BackchannelAuthority.Replace($"/realms/{realm}", "");
        var deleteUserUrl = $"{baseUrl}/admin/realms/{realm}/users/{keycloakUserId}";

        _logger.LogInformation("Deleting user from Keycloak: {KeycloakUserId}", keycloakUserId);

        var request = new HttpRequestMessage(HttpMethod.Delete, deleteUserUrl)
        {
            Headers = { { "Authorization", $"Bearer {adminAccessToken}" } }
        };

        var response = await httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Failed to delete user from Keycloak. Status: {StatusCode}, Body: {ErrorBody}",
                response.StatusCode, errorBody);
            throw new InvalidOperationException($"Failed to delete user from Keycloak: {response.StatusCode} - {errorBody}");
        }

        _logger.LogInformation("Successfully deleted user from Keycloak: {KeycloakUserId}", keycloakUserId);
    }

    private record KeycloakTokenResponse(
        [property: System.Text.Json.Serialization.JsonPropertyName("access_token")] string AccessToken,
        [property: System.Text.Json.Serialization.JsonPropertyName("token_type")] string TokenType,
        [property: System.Text.Json.Serialization.JsonPropertyName("expires_in")] int ExpiresIn,
        [property: System.Text.Json.Serialization.JsonPropertyName("refresh_token")] string? RefreshToken,
        [property: System.Text.Json.Serialization.JsonPropertyName("id_token")] string? IdToken);
}
