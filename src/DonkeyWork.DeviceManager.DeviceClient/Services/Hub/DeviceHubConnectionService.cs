using DonkeyWork.DeviceManager.DeviceClient.Configuration;
using DonkeyWork.DeviceManager.DeviceClient.Models;
using DonkeyWork.DeviceManager.DeviceClient.Services.Device;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;

namespace DonkeyWork.DeviceManager.DeviceClient.Services.Hub;

using DonkeyWork.DeviceManager.Common.Models.Commands;
using DonkeyWork.DeviceManager.Common.SignalR;

/// <summary>
/// Service for managing SignalR connection to the authenticated device hub.
/// </summary>
public class DeviceHubConnectionService : IDeviceHubConnectionService, IAsyncDisposable
{
    private readonly DeviceManagerConfiguration _config;
    private readonly ILogger<DeviceHubConnectionService> _logger;
    private readonly ISystemControlService _systemControl;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);

    private HubConnection? _hubConnection;
    private string? _currentAccessToken;

    public DeviceHubConnectionService(
        IOptions<DeviceManagerConfiguration> config,
        ILogger<DeviceHubConnectionService> logger,
        ISystemControlService systemControl)
    {
        _config = config.Value;
        _logger = logger;
        _systemControl = systemControl;

        _logger.LogDebug("Device hub connection service initialized with API base URL: {ApiBaseUrl}", _config.ApiBaseUrl);
    }

    public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

    public async Task ConnectAsync(DeviceTokens tokens, CancellationToken cancellationToken = default)
    {
        await _connectionLock.WaitAsync(cancellationToken);
        try
        {
            if (_hubConnection != null)
            {
                _logger.LogWarning("Hub connection already exists. Disconnecting first.");
                await DisconnectInternalAsync();
            }

            _currentAccessToken = tokens.AccessToken;

            _logger.LogInformation("Creating device hub connection");

            _hubConnection = new HubConnectionBuilder()
                .WithUrl($"{_config.ApiBaseUrl}/hubs/device", options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult(_currentAccessToken);
                })
                .WithAutomaticReconnect(new[] {
                    TimeSpan.FromSeconds(0),    // Reconnect immediately
                    TimeSpan.FromSeconds(2),    // Then after 2 seconds
                    TimeSpan.FromSeconds(10),   // Then after 10 seconds
                    TimeSpan.FromSeconds(30)    // Then after 30 seconds, then keep trying every 30 seconds
                })
                .Build();

            // Set up lifecycle event handlers
            _hubConnection.Closed += OnConnectionClosed;
            _hubConnection.Reconnecting += OnReconnecting;
            _hubConnection.Reconnected += OnReconnected;

            // Set up command handlers
            RegisterCommandHandlers();

            _logger.LogInformation("Starting device hub connection");
            await _hubConnection.StartAsync(cancellationToken);

            _logger.LogInformation("Successfully connected to device hub. Connection ID: {ConnectionId}",
                _hubConnection.ConnectionId);

            // Test connection with ping
            try
            {
                var pong = await PingAsync(cancellationToken);
                _logger.LogInformation("Connection verified: {Pong}", pong);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to ping server after connection");
            }
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        await _connectionLock.WaitAsync(cancellationToken);
        try
        {
            await DisconnectInternalAsync();
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    public async Task UpdateTokensAsync(DeviceTokens tokens, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating access token for device {DeviceUserId}", tokens.DeviceUserId);

        // Simply update the token - no need to reconnect
        // The existing connection is already authenticated and will continue to work
        // The new token will be used if/when automatic reconnection happens
        _currentAccessToken = tokens.AccessToken;

        _logger.LogDebug("Access token updated. Connection remains active.");

        await Task.CompletedTask;
    }

    public async Task ReportStatusAsync(string status, CancellationToken cancellationToken = default)
    {
        EnsureConnected();

        try
        {
            _logger.LogDebug("Reporting status: {Status}", status);
            await _hubConnection!.InvokeAsync(HubMethodNames.DeviceInvoke.ReportStatus, status, cancellationToken);
            _logger.LogDebug("Status reported successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reporting status");
            throw;
        }
    }

    public async Task<string> PingAsync(CancellationToken cancellationToken = default)
    {
        EnsureConnected();

        try
        {
            _logger.LogDebug("Pinging server");
            var response = await _hubConnection!.InvokeAsync<string>(HubMethodNames.DeviceInvoke.Ping, cancellationToken);
            _logger.LogDebug("Ping response: {Response}", response);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pinging server");
            throw;
        }
    }

    private async Task DisconnectInternalAsync()
    {
        if (_hubConnection == null)
        {
            return;
        }

        try
        {
            _logger.LogInformation("Disconnecting from device hub");

            // Unsubscribe from events
            _hubConnection.Closed -= OnConnectionClosed;
            _hubConnection.Reconnecting -= OnReconnecting;
            _hubConnection.Reconnected -= OnReconnected;

            if (_hubConnection.State != HubConnectionState.Disconnected)
            {
                await _hubConnection.StopAsync();
            }

            await _hubConnection.DisposeAsync();
            _hubConnection = null;

            _logger.LogInformation("Disconnected from device hub");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disconnecting from device hub");
            throw;
        }
    }

    private Task OnConnectionClosed(Exception? error)
    {
        if (error != null)
        {
            _logger.LogWarning(error, "Device hub connection closed with error");
        }
        else
        {
            _logger.LogInformation("Device hub connection closed");
        }

        return Task.CompletedTask;
    }

    private Task OnReconnecting(Exception? error)
    {
        _logger.LogWarning(error, "Device hub connection lost. Reconnecting...");
        return Task.CompletedTask;
    }

    private Task OnReconnected(string? connectionId)
    {
        _logger.LogInformation("Device hub reconnected. Connection ID: {ConnectionId}", connectionId);
        return Task.CompletedTask;
    }

    private void RegisterCommandHandlers()
    {
        if (_hubConnection == null)
        {
            throw new InvalidOperationException("Hub connection is null");
        }

        _logger.LogDebug("Registering command handlers");

        // Handler for ping commands from users
        _hubConnection.On<PingCommandData>(HubMethodNames.UserToDevice.ReceivePingCommand, async (command) =>
        {
            _logger.LogInformation("Received ping command from user {RequestedBy}, CommandId: {CommandId}",
                command.RequestedBy, command.CommandId);

            try
            {
                // Measure latency (simulate processing)
                var startTime = DateTimeOffset.UtcNow;
                await Task.Delay(10); // Simulate some processing
                var latencyMs = (int)(DateTimeOffset.UtcNow - startTime).TotalMilliseconds;

                // Send response back through hub
                await _hubConnection.InvokeAsync(HubMethodNames.DeviceInvoke.SendPingResponse, command.CommandId, latencyMs);

                _logger.LogInformation("Sent ping response for command {CommandId} with latency {LatencyMs}ms",
                    command.CommandId, latencyMs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling ping command {CommandId}", command.CommandId);
            }
        });

        // Handler for shutdown commands
        _hubConnection.On<CommandData>(HubMethodNames.UserToDevice.ReceiveShutdownCommand, async (command) =>
        {
            _logger.LogWarning("Received shutdown command from user {RequestedBy}, CommandId: {CommandId}",
                command.RequestedBy, command.CommandId);

            try
            {
                // Acknowledge the command
                await _hubConnection.InvokeAsync(HubMethodNames.DeviceInvoke.AcknowledgeCommand,
                    command.CommandId, "shutdown", true, "Shutdown initiated");

                _logger.LogInformation("Acknowledged shutdown command {CommandId}", command.CommandId);

                // Give a moment for the acknowledgment to be sent before shutting down
                await Task.Delay(500);

                // Execute shutdown
                _logger.LogWarning("Initiating system shutdown");
                await _systemControl.ShutdownAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling shutdown command {CommandId}", command.CommandId);

                try
                {
                    await _hubConnection.InvokeAsync(HubMethodNames.DeviceInvoke.AcknowledgeCommand,
                        command.CommandId, "shutdown", false, $"Error: {ex.Message}");
                }
                catch
                {
                    // Ignore errors sending failure acknowledgment
                }
            }
        });

        // Handler for restart commands
        _hubConnection.On<CommandData>(HubMethodNames.UserToDevice.ReceiveRestartCommand, async (command) =>
        {
            _logger.LogWarning("Received restart command from user {RequestedBy}, CommandId: {CommandId}",
                command.RequestedBy, command.CommandId);

            try
            {
                // Acknowledge the command
                await _hubConnection.InvokeAsync(HubMethodNames.DeviceInvoke.AcknowledgeCommand,
                    command.CommandId, "restart", true, "Restart initiated");

                _logger.LogInformation("Acknowledged restart command {CommandId}", command.CommandId);

                // Give a moment for the acknowledgment to be sent before restarting
                await Task.Delay(500);

                // Execute restart
                _logger.LogWarning("Initiating system restart");
                await _systemControl.RestartAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling restart command {CommandId}", command.CommandId);

                try
                {
                    await _hubConnection.InvokeAsync(HubMethodNames.DeviceInvoke.AcknowledgeCommand,
                        command.CommandId, "restart", false, $"Error: {ex.Message}");
                }
                catch
                {
                    // Ignore errors sending failure acknowledgment
                }
            }
        });

        _logger.LogInformation("Command handlers registered successfully");
    }

    private void EnsureConnected()
    {
        if (_hubConnection == null || _hubConnection.State != HubConnectionState.Connected)
        {
            throw new InvalidOperationException("Hub connection is not established. Call ConnectAsync first.");
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
        _connectionLock.Dispose();
    }
}
