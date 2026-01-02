namespace DonkeyWork.DeviceManager.Api.Services;

public interface IUserAuthService
{
    /// <summary>
    /// Exchanges the authorization code for tokens from Keycloak.
    /// </summary>
    /// <param name="code">The authorization code from the OAuth flow</param>
    /// <param name="state">The state parameter for CSRF protection</param>
    /// <param name="redirectUri">The redirect URI that was used in the authorization request</param>
    /// <returns>Token response from Keycloak</returns>
    Task<TokenResponse> AuthorizeUserAsync(string code, string state, string redirectUri);
}

public record TokenResponse(
    string AccessToken,
    string TokenType,
    int ExpiresIn,
    string? RefreshToken,
    string? IdToken);