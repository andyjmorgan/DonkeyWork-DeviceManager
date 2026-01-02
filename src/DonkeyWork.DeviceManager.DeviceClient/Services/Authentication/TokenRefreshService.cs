using System.Text.Json;
using DonkeyWork.DeviceManager.Common.Models.Device;
using DonkeyWork.DeviceManager.DeviceClient.Configuration;
using DonkeyWork.DeviceManager.DeviceClient.Models;
using Microsoft.Extensions.Options;

namespace DonkeyWork.DeviceManager.DeviceClient.Services.Authentication;

/// <summary>
/// Service for refreshing device authentication tokens via the backend API.
/// </summary>
public class TokenRefreshService : ITokenRefreshService
{
    private readonly DeviceManagerConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TokenRefreshService> _logger;

    public TokenRefreshService(
        IOptions<DeviceManagerConfiguration> config,
        IHttpClientFactory httpClientFactory,
        ILogger<TokenRefreshService> logger)
    {
        _config = config.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;

        _logger.LogDebug("Token refresh service initialized with API base URL: {ApiBaseUrl}", _config.ApiBaseUrl);
    }

    public async Task<DeviceTokens?> RefreshAccessTokenAsync(
        DeviceTokens currentTokens,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Refreshing access token for device {DeviceUserId} via backend API", currentTokens.DeviceUserId);

            var refreshEndpoint = $"{_config.ApiBaseUrl}/api/device/refresh";
            _logger.LogDebug("Calling refresh endpoint: {RefreshEndpoint}", refreshEndpoint);

            var httpClient = _httpClientFactory.CreateClient();

            var request = new RefreshTokenRequest
            {
                RefreshToken = currentTokens.RefreshToken
            };

            var requestJson = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var content = new StringContent(requestJson, System.Text.Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(refreshEndpoint, content, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Token refresh failed with status {StatusCode}: {Response}",
                    response.StatusCode, responseBody);
                return null;
            }

            var refreshResponse = JsonSerializer.Deserialize<RefreshTokenResponse>(responseBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (refreshResponse == null)
            {
                _logger.LogError("Failed to deserialize refresh token response");
                return null;
            }

            // Calculate expiry time
            var expiresAtUtc = DateTime.UtcNow.AddSeconds(refreshResponse.ExpiresIn);

            var updatedTokens = new DeviceTokens
            {
                AccessToken = refreshResponse.AccessToken,
                RefreshToken = refreshResponse.RefreshToken,
                DeviceUserId = currentTokens.DeviceUserId,
                TenantId = currentTokens.TenantId,
                ExpiresAtUtc = expiresAtUtc
            };

            _logger.LogInformation("Successfully refreshed access token via backend API. Expires at {ExpiresAtUtc:O}", expiresAtUtc);

            return updatedTokens;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Token refresh was cancelled");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing access token via backend API");
            return null;
        }
    }
}
