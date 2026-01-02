using System.IdentityModel.Tokens.Jwt;
using DonkeyWork.DeviceManager.Common.Models.Registration;
using DonkeyWork.DeviceManager.DeviceClient.Configuration;
using DonkeyWork.DeviceManager.DeviceClient.Models;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;

namespace DonkeyWork.DeviceManager.DeviceClient.Services.Hub;

/// <summary>
/// Service for handling device registration via SignalR.
/// </summary>
public class DeviceRegistrationService : IDeviceRegistrationService
{
    private readonly DeviceManagerConfiguration _config;
    private readonly ILogger<DeviceRegistrationService> _logger;

    public DeviceRegistrationService(
        IOptions<DeviceManagerConfiguration> config,
        ILogger<DeviceRegistrationService> logger)
    {
        _config = config.Value;
        _logger = logger;

        _logger.LogDebug("Device registration service initialized with API base URL: {ApiBaseUrl}", _config.ApiBaseUrl);
    }

    public async Task<DeviceTokens?> RegisterDeviceAsync(
        int timeoutMinutes = 5,
        CancellationToken cancellationToken = default)
    {
        HubConnection? registrationHub = null;

        try
        {
            _logger.LogInformation("Starting device registration process");

            // Connect to registration hub
            _logger.LogDebug("Connecting to registration hub at {Url}", $"{_config.ApiBaseUrl}/hubs/device-registration");

            registrationHub = new HubConnectionBuilder()
                .WithUrl($"{_config.ApiBaseUrl}/hubs/device-registration")
                .WithAutomaticReconnect()
                .Build();

            // Set up semaphore to wait for credentials
            var credentialsReceived = new SemaphoreSlim(0, 1);
            DeviceCredentialsResponse? receivedCredentials = null;

            // Register handler for receiving credentials
            registrationHub.On<DeviceCredentialsResponse>("ReceiveCredentials", credentials =>
            {
                _logger.LogInformation("Received credentials from server. Device ID: {DeviceUserId}, Tenant ID: {TenantId}",
                    credentials.DeviceUserId, credentials.TenantId);

                receivedCredentials = credentials;
                credentialsReceived.Release();
            });

            await registrationHub.StartAsync(cancellationToken);
            _logger.LogInformation("Connected to registration hub");

            // Request registration
            _logger.LogDebug("Requesting device registration");
            var registration = await registrationHub.InvokeAsync<DeviceRegistrationResponse>(
                "RequestRegistration",
                cancellationToken);

            _logger.LogInformation("Registration initiated. Registration ID: {RegistrationId}", registration.RegistrationId);

            // Display three-word code
            _logger.LogWarning("═══════════════════════════════════════════════════════════");
            _logger.LogWarning("  THREE-WORD CODE: {ThreeWordCode}", registration.ThreeWordCode);
            _logger.LogWarning("  Please enter this code in the web portal to complete registration");
            _logger.LogWarning("═══════════════════════════════════════════════════════════");

            // Wait for credentials with timeout
            _logger.LogInformation("Waiting for registration completion (timeout: {TimeoutMinutes} minutes)", timeoutMinutes);

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromMinutes(timeoutMinutes));

            try
            {
                await credentialsReceived.WaitAsync(timeoutCts.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Registration timed out after {TimeoutMinutes} minutes", timeoutMinutes);
                return null;
            }

            if (receivedCredentials == null)
            {
                _logger.LogWarning("Registration completed but no credentials received");
                return null;
            }

            // Parse expiry from access token
            var expiresAtUtc = ExtractExpiryFromToken(receivedCredentials.AccessToken);

            var tokens = new DeviceTokens
            {
                AccessToken = receivedCredentials.AccessToken,
                RefreshToken = receivedCredentials.RefreshToken,
                DeviceUserId = receivedCredentials.DeviceUserId,
                TenantId = receivedCredentials.TenantId,
                ExpiresAtUtc = expiresAtUtc
            };

            _logger.LogInformation("Device registration completed successfully. Token expires at {ExpiresAtUtc:O}",
                expiresAtUtc);

            return tokens;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Device registration was cancelled");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during device registration");
            return null;
        }
        finally
        {
            if (registrationHub != null)
            {
                try
                {
                    await registrationHub.StopAsync(CancellationToken.None);
                    await registrationHub.DisposeAsync();
                    _logger.LogDebug("Registration hub connection closed");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error closing registration hub connection");
                }
            }
        }
    }

    private DateTime ExtractExpiryFromToken(string accessToken)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(accessToken);

            if (token.ValidTo != DateTime.MinValue)
            {
                return token.ValidTo;
            }

            _logger.LogWarning("Token does not contain valid expiry, using default of 1 hour from now");
            return DateTime.UtcNow.AddHours(1);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing JWT token expiry, using default of 1 hour from now");
            return DateTime.UtcNow.AddHours(1);
        }
    }
}
