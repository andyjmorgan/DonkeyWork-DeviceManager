using DonkeyWork.DeviceManager.DeviceClient.Services.Storage;

namespace DonkeyWork.DeviceManager.DeviceClient.Services.Api;

/// <summary>
/// HTTP message handler that adds Authorization header with bearer token to outgoing requests.
/// </summary>
public class AuthorizationMessageHandler : DelegatingHandler
{
    private readonly ITokenStorageService _tokenStorage;
    private readonly ILogger<AuthorizationMessageHandler> _logger;

    public AuthorizationMessageHandler(
        ITokenStorageService tokenStorage,
        ILogger<AuthorizationMessageHandler> logger)
    {
        _tokenStorage = tokenStorage;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Load current tokens
        var tokens = await _tokenStorage.LoadTokensAsync();

        if (tokens != null && !string.IsNullOrEmpty(tokens.AccessToken))
        {
            // Add Bearer token to Authorization header
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer",
                tokens.AccessToken);

            _logger.LogDebug("Added Authorization header to request to {RequestUri}", request.RequestUri);
        }
        else
        {
            _logger.LogWarning("No access token available for request to {RequestUri}", request.RequestUri);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
