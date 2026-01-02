namespace DonkeyWork.DeviceManager.Api.Services;

using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DonkeyWork.DeviceManager.Api.Configuration;
using DonkeyWork.DeviceManager.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class UserAuthService : IUserAuthService
{
    private readonly KeycloakConfiguration _keycloakConfig;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly DeviceManagerContext _dbContext;
    private readonly ITenantProvisioningService _tenantProvisioningService;
    private readonly ILogger<UserAuthService> _logger;

    public UserAuthService(
        IOptions<KeycloakConfiguration> keycloakConfig,
        IHttpClientFactory httpClientFactory,
        DeviceManagerContext dbContext,
        ITenantProvisioningService tenantProvisioningService,
        ILogger<UserAuthService> logger)
    {
        _keycloakConfig = keycloakConfig.Value;
        _httpClientFactory = httpClientFactory;
        _dbContext = dbContext;
        _tenantProvisioningService = tenantProvisioningService;
        _logger = logger;
    }

    public async Task<TokenResponse> AuthorizeUserAsync(string code, string state, string redirectUri)
    {
        // Use InternalAuthority for backchannel calls if available (to avoid hairpin NAT in k8s)
        // Fall back to Authority if InternalAuthority is not set (e.g., local development)
        var backchannelAuthority = _keycloakConfig.InternalAuthority ?? _keycloakConfig.Authority;
        var usingInternalAuthority = _keycloakConfig.InternalAuthority != null;

        _logger.LogInformation("Using {AuthorityType} for backchannel calls: {Authority}",
            usingInternalAuthority ? "InternalAuthority" : "Authority",
            backchannelAuthority);

        _logger.LogInformation("Using redirectUri: {RedirectUri}", redirectUri);

        var authorityUri = new Uri(backchannelAuthority);
        var tokenEndpoint = $"{authorityUri.GetLeftPart(UriPartial.Authority)}{authorityUri.AbsolutePath}/protocol/openid-connect/token";

        var httpClient = _httpClientFactory.CreateClient();

        // Prepare token exchange request
        // Use the redirectUri from the request (matches what the frontend sent to Keycloak)
        var tokenRequest = new Dictionary<string, string>
        {
            { "grant_type", "authorization_code" },
            { "code", code },
            { "client_id", _keycloakConfig.ClientId },
            { "client_secret", _keycloakConfig.ClientSecret },
            { "redirect_uri", redirectUri }
        };

        var content = new FormUrlEncodedContent(tokenRequest);

        try
        {
            _logger.LogInformation("Exchanging authorization code for tokens at {TokenEndpoint}", tokenEndpoint);

            var response = await httpClient.PostAsync(tokenEndpoint, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Token exchange failed with status {StatusCode}: {Response}",
                    response.StatusCode, responseBody);
                throw new InvalidOperationException($"Token exchange failed: {response.StatusCode}");
            }

            var tokenData = JsonSerializer.Deserialize<KeycloakTokenResponse>(responseBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (tokenData == null)
            {
                throw new InvalidOperationException("Failed to deserialize token response");
            }

            _logger.LogInformation("Successfully exchanged authorization code for tokens");

            // Step 1: Decode JWT to extract user information
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(tokenData.AccessToken);

            var keycloakUserId = jwtToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
            var email = jwtToken.Claims.FirstOrDefault(c => c.Type == "email")?.Value ?? "unknown@example.com";
            var name = jwtToken.Claims.FirstOrDefault(c => c.Type == "name")?.Value ?? "Unknown User";
            var tenantIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "tenantId")?.Value;

            if (string.IsNullOrEmpty(keycloakUserId))
            {
                throw new InvalidOperationException("Failed to extract user ID from token");
            }

            var keycloakUserGuid = Guid.Parse(keycloakUserId);

            // Step 2: Check if user exists in database, create if not
            var existingUser = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Id == keycloakUserGuid);

            if (existingUser == null)
            {
                _logger.LogInformation("User {UserId} (Keycloak ID) not found in database. Will be created during tenant provisioning.", keycloakUserGuid);
            }
            else
            {
                _logger.LogInformation("User {UserId} (Keycloak ID) found in database", existingUser.Id);
            }

            // Step 3: Check if tenantId exists in JWT
            if (string.IsNullOrEmpty(tenantIdClaim))
            {
                _logger.LogInformation("No tenantId found in JWT for user {UserId}. Starting provisioning process.", keycloakUserId);

                // Provision tenant and get the new tenantId
                var newTenantId = await ProvisionTenantForUserAsync(keycloakUserId, email, name);

                // Step 4: Refresh token to include the new tenantId
                if (tokenData.RefreshToken != null)
                {
                    _logger.LogInformation("Refreshing token to include new tenantId {TenantId}", newTenantId);
                    var refreshedTokens = await RefreshTokenAsync(tokenData.RefreshToken);

                    return refreshedTokens;
                }
                else
                {
                    _logger.LogWarning("No refresh token available. Returning original tokens. User should re-login to get tenantId in token.");
                }
            }
            else
            {
                _logger.LogInformation("User {UserId} already has tenantId in JWT: {TenantId}", keycloakUserId, tenantIdClaim);
            }

            return new TokenResponse(
                AccessToken: tokenData.AccessToken,
                TokenType: tokenData.TokenType,
                ExpiresIn: tokenData.ExpiresIn,
                RefreshToken: tokenData.RefreshToken,
                IdToken: tokenData.IdToken
            );
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error during token exchange");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during token exchange");
            throw;
        }
    }

    private async Task<Guid> ProvisionTenantForUserAsync(string keycloakUserId, string email, string name)
    {
        try
        {
            var realm = ExtractRealm(_keycloakConfig.Authority);
            var httpClient = _httpClientFactory.CreateClient();

            // Get admin access token
            var adminToken = await GetAdminAccessTokenAsync(httpClient);

            // Get user from Keycloak Admin API (use internal authority for backchannel calls)
            var backchannelAuthority = _keycloakConfig.InternalAuthority ?? _keycloakConfig.Authority;
            var usingInternalAuthority = _keycloakConfig.InternalAuthority != null;

            _logger.LogInformation("Using {AuthorityType} for Keycloak Admin API calls: {Authority}",
                usingInternalAuthority ? "InternalAuthority" : "Authority",
                backchannelAuthority);

            var authorityUri = new Uri(backchannelAuthority);
            var baseUrl = $"{authorityUri.GetLeftPart(UriPartial.Authority)}";
            var getUserUrl = $"{baseUrl}/admin/realms/{realm}/users/{keycloakUserId}";

            var getUserRequest = new HttpRequestMessage(HttpMethod.Get, getUserUrl);
            getUserRequest.Headers.Add("Authorization", $"Bearer {adminToken}");

            var getUserResponse = await httpClient.SendAsync(getUserRequest);

            if (!getUserResponse.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Failed to get user from Keycloak: {getUserResponse.StatusCode}");
            }

            var userJson = await getUserResponse.Content.ReadAsStringAsync();
            var user = JsonSerializer.Deserialize<KeycloakUser>(userJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (user == null)
            {
                throw new InvalidOperationException("User not found in Keycloak");
            }

            // Generate new tenantId
            var tenantId = Guid.NewGuid();
            _logger.LogInformation("Generating new tenantId {TenantId} for user {UserId}", tenantId, keycloakUserId);

            // Update user attributes in Keycloak
            user.Attributes ??= new Dictionary<string, List<string>>();
            user.Attributes["tenantId"] = new List<string> { tenantId.ToString() };

            var updateUserUrl = $"{baseUrl}/admin/realms/{realm}/users/{keycloakUserId}";
            var updateUserRequest = new HttpRequestMessage(HttpMethod.Put, updateUserUrl);
            updateUserRequest.Headers.Add("Authorization", $"Bearer {adminToken}");
            updateUserRequest.Content = new StringContent(
                JsonSerializer.Serialize(user),
                Encoding.UTF8,
                "application/json");

            var updateUserResponse = await httpClient.SendAsync(updateUserRequest);

            if (!updateUserResponse.IsSuccessStatusCode)
            {
                var errorBody = await updateUserResponse.Content.ReadAsStringAsync();
                _logger.LogError("Failed to update user {UserId} in Keycloak: {StatusCode} - {Error}",
                    keycloakUserId, updateUserResponse.StatusCode, errorBody);
                throw new InvalidOperationException($"Failed to update user in Keycloak: {updateUserResponse.StatusCode}");
            }

            _logger.LogInformation("Updated user {UserId} with tenantId {TenantId} in Keycloak", keycloakUserId, tenantId);

            // Provision tenant, user, building, and room synchronously
            await _tenantProvisioningService.ProvisionTenantAsync(tenantId, keycloakUserId, email, name);

            _logger.LogInformation("Completed tenant provisioning for tenantId {TenantId}", tenantId);

            return tenantId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error provisioning tenant for user {UserId}", keycloakUserId);
            throw;
        }
    }

    private async Task<string> GetAdminAccessTokenAsync(HttpClient httpClient)
    {
        // Use internal authority for backchannel token requests
        var backchannelAuthority = _keycloakConfig.InternalAuthority ?? _keycloakConfig.Authority;
        var usingInternalAuthority = _keycloakConfig.InternalAuthority != null;

        _logger.LogInformation("Using {AuthorityType} for admin token request: {Authority}",
            usingInternalAuthority ? "InternalAuthority" : "Authority",
            backchannelAuthority);

        var authorityUri = new Uri(backchannelAuthority);
        var tokenEndpoint = $"{authorityUri.GetLeftPart(UriPartial.Authority)}{authorityUri.AbsolutePath}/protocol/openid-connect/token";

        var tokenRequest = new Dictionary<string, string>
        {
            { "grant_type", "client_credentials" },
            { "client_id", _keycloakConfig.AdminClientId },
            { "client_secret", _keycloakConfig.AdminClientSecret }
        };

        var content = new FormUrlEncodedContent(tokenRequest);
        var response = await httpClient.PostAsync(tokenEndpoint, content);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            _logger.LogError("Failed to get admin access token: {StatusCode} - {Error}",
                response.StatusCode, errorBody);
            throw new InvalidOperationException("Failed to get admin access token");
        }

        var responseBody = await response.Content.ReadAsStringAsync();
        var tokenData = JsonSerializer.Deserialize<KeycloakTokenResponse>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return tokenData?.AccessToken ?? throw new InvalidOperationException("No access token in response");
    }

    private async Task<TokenResponse> RefreshTokenAsync(string refreshToken)
    {
        // Use internal authority for backchannel token refresh
        var backchannelAuthority = _keycloakConfig.InternalAuthority ?? _keycloakConfig.Authority;
        var usingInternalAuthority = _keycloakConfig.InternalAuthority != null;

        _logger.LogInformation("Using {AuthorityType} for token refresh: {Authority}",
            usingInternalAuthority ? "InternalAuthority" : "Authority",
            backchannelAuthority);

        var authorityUri = new Uri(backchannelAuthority);
        var tokenEndpoint = $"{authorityUri.GetLeftPart(UriPartial.Authority)}{authorityUri.AbsolutePath}/protocol/openid-connect/token";

        var httpClient = _httpClientFactory.CreateClient();

        var tokenRequest = new Dictionary<string, string>
        {
            { "grant_type", "refresh_token" },
            { "refresh_token", refreshToken },
            { "client_id", _keycloakConfig.ClientId },
            { "client_secret", _keycloakConfig.ClientSecret }
        };

        var content = new FormUrlEncodedContent(tokenRequest);

        try
        {
            _logger.LogInformation("Refreshing token");

            var response = await httpClient.PostAsync(tokenEndpoint, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Token refresh failed with status {StatusCode}: {Response}",
                    response.StatusCode, responseBody);
                throw new InvalidOperationException($"Token refresh failed: {response.StatusCode}");
            }

            var tokenData = JsonSerializer.Deserialize<KeycloakTokenResponse>(responseBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (tokenData == null)
            {
                throw new InvalidOperationException("Failed to deserialize token response");
            }

            _logger.LogInformation("Successfully refreshed token");

            return new TokenResponse(
                AccessToken: tokenData.AccessToken,
                TokenType: tokenData.TokenType,
                ExpiresIn: tokenData.ExpiresIn,
                RefreshToken: tokenData.RefreshToken,
                IdToken: tokenData.IdToken
            );
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error during token refresh");
            throw;
        }
    }

    private static string ExtractRealm(string authority)
    {
        // Extract realm from: https://auth.donkeywork.dev/realms/DeviceManager
        var uri = new Uri(authority);
        var segments = uri.Segments;
        return segments[^1].TrimEnd('/');
    }

    // todo: this should be it's own class file
    private record KeycloakTokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("token_type")] string TokenType,
        [property: JsonPropertyName("expires_in")] int ExpiresIn,
        [property: JsonPropertyName("refresh_token")] string? RefreshToken,
        [property: JsonPropertyName("id_token")] string? IdToken);

    // todo: this should be it's own class file
    private class KeycloakUser
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("username")]
        public string? Username { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("firstName")]
        public string? FirstName { get; set; }

        [JsonPropertyName("lastName")]
        public string? LastName { get; set; }

        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; }

        [JsonPropertyName("emailVerified")]
        public bool EmailVerified { get; set; }

        [JsonPropertyName("attributes")]
        public Dictionary<string, List<string>>? Attributes { get; set; }
    }
}
