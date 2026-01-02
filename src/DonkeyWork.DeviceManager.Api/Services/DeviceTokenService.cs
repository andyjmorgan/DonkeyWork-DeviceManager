using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using System.Text.Json.Serialization;
using DonkeyWork.DeviceManager.Api.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DonkeyWork.DeviceManager.Api.Services;

/// <summary>
/// Service for managing device authentication tokens via Keycloak.
/// </summary>
public class DeviceTokenService : IDeviceTokenService
{
    private readonly KeycloakConfiguration _keycloakConfig;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<DeviceTokenService> _logger;

    public DeviceTokenService(
        IOptions<KeycloakConfiguration> keycloakConfig,
        IHttpClientFactory httpClientFactory,
        ILogger<DeviceTokenService> logger)
    {
        _keycloakConfig = keycloakConfig.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<(string AccessToken, string RefreshToken, int ExpiresIn)?> RefreshDeviceTokenAsync(string refreshToken)
    {
        try
        {
            _logger.LogInformation("Refreshing device token");

            var authorityUri = new Uri(_keycloakConfig.Authority);
            var tokenEndpoint = $"{authorityUri.GetLeftPart(UriPartial.Authority)}{authorityUri.AbsolutePath}/protocol/openid-connect/token";

            var httpClient = _httpClientFactory.CreateClient();

            // Confidential client token refresh (includes client secret)
            // Devices were created using the main client, so we use the same client credentials
            var tokenRequest = new Dictionary<string, string>
            {
                { "grant_type", "refresh_token" },
                { "refresh_token", refreshToken },
                { "client_id", _keycloakConfig.ClientId },
                { "client_secret", _keycloakConfig.ClientSecret }
            };

            var content = new FormUrlEncodedContent(tokenRequest);

            var response = await httpClient.PostAsync(tokenEndpoint, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Token refresh failed with status {StatusCode}: {Response}",
                    response.StatusCode, responseBody);
                return null;
            }

            var tokenData = JsonSerializer.Deserialize<KeycloakTokenResponse>(responseBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (tokenData == null)
            {
                _logger.LogError("Failed to deserialize Keycloak token response");
                return null;
            }

            _logger.LogInformation("Successfully refreshed device token. Expires in {ExpiresIn} seconds", tokenData.ExpiresIn);

            return (tokenData.AccessToken, tokenData.RefreshToken ?? refreshToken, tokenData.ExpiresIn);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing device token");
            return null;
        }
    }

    private string? ExtractClientIdFromToken(string refreshToken)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(refreshToken);

            // Try azp (authorized party) claim first - this is the standard Keycloak claim for client_id
            var clientId = token.Claims.FirstOrDefault(c => c.Type == "azp")?.Value;

            if (!string.IsNullOrEmpty(clientId))
            {
                return clientId;
            }

            // Fallback to aud (audience) claim if azp is not present
            clientId = token.Claims.FirstOrDefault(c => c.Type == "aud")?.Value;

            return !string.IsNullOrEmpty(clientId) ? clientId : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error extracting client_id from refresh token");
            return null;
        }
    }

    private record KeycloakTokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("token_type")] string TokenType,
        [property: JsonPropertyName("expires_in")] int ExpiresIn,
        [property: JsonPropertyName("refresh_token")] string? RefreshToken,
        [property: JsonPropertyName("id_token")] string? IdToken);
}
