using DonkeyWork.DeviceManager.DeviceClient.Models;
using DonkeyWork.DeviceManager.DeviceClient.Services.Storage;
using DonkeyWork.DeviceManager.DeviceClient.Services.Hub;
using DonkeyWork.DeviceManager.DeviceClient.Services.Authentication;
using DonkeyWork.DeviceManager.DeviceClient.Services.Api;
using DonkeyWork.DeviceManager.DeviceClient.Services.Device;

namespace DonkeyWork.DeviceManager.DeviceClient;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly ITokenStorageService _tokenStorage;
    private readonly IDeviceRegistrationService _registrationService;
    private readonly ITokenRefreshService _tokenRefreshService;
    private readonly IDeviceHubConnectionService _hubConnection;
    private readonly IDeviceManagerApi _deviceApi;
    private readonly IDeviceInformationDiscoveryService _deviceInfo;

    // Timers and intervals
    private const int TokenCheckIntervalSeconds = 60; // Check token every minute
    private const int StatusReportIntervalSeconds = 300; // Report status every 5 minutes
    private const int TokenRefreshBufferMinutes = 5; // Refresh token 5 minutes before expiry

    private DeviceTokens? _currentTokens;

    public Worker(
        ILogger<Worker> logger,
        ITokenStorageService tokenStorage,
        IDeviceRegistrationService registrationService,
        ITokenRefreshService tokenRefreshService,
        IDeviceHubConnectionService hubConnection,
        IDeviceManagerApi deviceApi,
        IDeviceInformationDiscoveryService deviceInfo)
    {
        _logger = logger;
        _tokenStorage = tokenStorage;
        _registrationService = registrationService;
        _tokenRefreshService = tokenRefreshService;
        _hubConnection = hubConnection;
        _deviceApi = deviceApi;
        _deviceInfo = deviceInfo;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DonkeyWork Device Manager Client starting");

        try
        {
            // Phase 1: Ensure we have valid tokens
            _currentTokens = await EnsureValidTokensAsync(stoppingToken);

            if (_currentTokens == null)
            {
                _logger.LogError("Failed to obtain device tokens. Exiting.");
                return;
            }

            // Phase 2: Connect to device hub
            await _hubConnection.ConnectAsync(_currentTokens, stoppingToken);

            // Phase 3: Post device information
            await PostDeviceInformationAsync(stoppingToken);

            // Phase 4: Main operation loop
            await RunMainLoopAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Device client shutdown requested");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Fatal error in device client");
            throw;
        }
        finally
        {
            // Cleanup
            if (_hubConnection.IsConnected)
            {
                try
                {
                    _logger.LogInformation("Reporting offline status before shutdown");
                    await _hubConnection.ReportStatusAsync("offline", CancellationToken.None);
                    await _hubConnection.DisconnectAsync(CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error during shutdown cleanup");
                }
            }

            _logger.LogInformation("DonkeyWork Device Manager Client stopped");
        }
    }

    private async Task<DeviceTokens?> EnsureValidTokensAsync(CancellationToken stoppingToken)
    {
        // Try to load existing tokens
        var tokens = await _tokenStorage.LoadTokensAsync();

        if (tokens == null)
        {
            _logger.LogInformation("No existing tokens found. Starting registration process.");
            return await RegisterDeviceAsync(stoppingToken);
        }

        _logger.LogInformation("Found existing tokens. Device ID: {DeviceUserId}, Tenant ID: {TenantId}",
            tokens.DeviceUserId, tokens.TenantId);

        // Check if token is expired or expiring soon
        if (tokens.IsExpiredOrExpiringSoon(TokenRefreshBufferMinutes))
        {
            _logger.LogInformation("Access token expired or expiring soon. Refreshing...");
            return await RefreshTokensAsync(tokens, stoppingToken);
        }

        _logger.LogInformation("Access token is valid. Expires at {ExpiresAtUtc:O}", tokens.ExpiresAtUtc);
        return tokens;
    }

    private async Task<DeviceTokens?> RegisterDeviceAsync(CancellationToken stoppingToken)
    {
        try
        {
            var tokens = await _registrationService.RegisterDeviceAsync(
                timeoutMinutes: 10,
                cancellationToken: stoppingToken);

            if (tokens == null)
            {
                _logger.LogError("Device registration failed or timed out");
                return null;
            }

            // Save tokens to file
            await _tokenStorage.SaveTokensAsync(tokens);
            _logger.LogInformation("Device registration successful and tokens saved");

            return tokens;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during device registration");
            return null;
        }
    }

    private async Task<DeviceTokens?> RefreshTokensAsync(DeviceTokens currentTokens, CancellationToken stoppingToken)
    {
        try
        {
            var refreshedTokens = await _tokenRefreshService.RefreshAccessTokenAsync(currentTokens, stoppingToken);

            if (refreshedTokens == null)
            {
                _logger.LogWarning(
                    "Token refresh failed. This may be due to network issues or backend unavailability. " +
                    "Keeping current tokens and will retry on next cycle. DO NOT DELETE LOCAL TOKENS.");
                return currentTokens;
            }

            // Save refreshed tokens
            await _tokenStorage.SaveTokensAsync(refreshedTokens);
            _logger.LogInformation("Access token refreshed and saved");

            return refreshedTokens;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing tokens. Keeping current tokens and will retry later.");
            return currentTokens;
        }
    }

    private async Task RunMainLoopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Entering main operation loop");

        var lastTokenCheck = DateTime.UtcNow;
        var lastStatusReport = DateTime.UtcNow;

        // Report initial online status
        try
        {
            await _hubConnection.ReportStatusAsync("online", stoppingToken);
            _logger.LogInformation("Reported online status");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to report initial online status");
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;

                // Check if we need to refresh the token
                if ((now - lastTokenCheck).TotalSeconds >= TokenCheckIntervalSeconds)
                {
                    lastTokenCheck = now;

                    if (_currentTokens!.IsExpiredOrExpiringSoon(TokenRefreshBufferMinutes))
                    {
                        _logger.LogInformation("Token expiring soon, refreshing...");

                        var refreshedTokens = await RefreshTokensAsync(_currentTokens, stoppingToken);

                        if (refreshedTokens != null)
                        {
                            _currentTokens = refreshedTokens;

                            // Update hub connection with new token
                            await _hubConnection.UpdateTokensAsync(_currentTokens, stoppingToken);
                        }
                        else
                        {
                            _logger.LogError("Failed to refresh tokens in main loop");
                        }
                    }
                }

                // Periodically report status
                if ((now - lastStatusReport).TotalSeconds >= StatusReportIntervalSeconds)
                {
                    lastStatusReport = now;

                    if (_hubConnection.IsConnected)
                    {
                        try
                        {
                            await _hubConnection.ReportStatusAsync("online", stoppingToken);
                            _logger.LogDebug("Reported online status");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to report status");
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Hub connection is not connected. Automatic reconnection should handle this.");
                    }
                }

                // Sleep for a short interval
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in main loop");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }

    private async Task PostDeviceInformationAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Collecting device information");

            // Discover device information
            var deviceInfo = _deviceInfo.DiscoverDeviceInformation();

            _logger.LogInformation(
                "Posting device information to backend: {CpuCores} cores, {MemoryGB:F2} GB RAM, {OS} {OSVersion}",
                deviceInfo.CpuCores,
                deviceInfo.TotalMemoryBytes / (1024.0 * 1024.0 * 1024.0),
                deviceInfo.OperatingSystem,
                deviceInfo.OperatingSystemVersion);

            // Post to backend API
            var response = await _deviceApi.PostDeviceInformationAsync(deviceInfo, stoppingToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully posted device information to backend");
            }
            else
            {
                _logger.LogWarning(
                    "Failed to post device information. Status: {StatusCode}, Error: {Error}",
                    response.StatusCode,
                    response.Error?.Content);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error posting device information");
            // Don't throw - this is non-critical, continue with main loop
        }
    }
}