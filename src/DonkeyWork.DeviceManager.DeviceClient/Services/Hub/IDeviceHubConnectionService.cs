using DonkeyWork.DeviceManager.DeviceClient.Models;

namespace DonkeyWork.DeviceManager.DeviceClient.Services.Hub;

/// <summary>
/// Service for managing SignalR connection to the authenticated device hub.
/// </summary>
public interface IDeviceHubConnectionService
{
    /// <summary>
    /// Gets a value indicating whether the hub is currently connected.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Connects to the device hub using the provided tokens.
    /// </summary>
    /// <param name="tokens">Device tokens for authentication.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ConnectAsync(DeviceTokens tokens, CancellationToken cancellationToken = default);

    /// <summary>
    /// Disconnects from the device hub.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DisconnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the access token used for hub authentication.
    /// This will trigger a reconnection with the new token.
    /// </summary>
    /// <param name="tokens">Updated device tokens.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateTokensAsync(DeviceTokens tokens, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reports device status to the server.
    /// </summary>
    /// <param name="status">Status message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ReportStatusAsync(string status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pings the server to test connectivity.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Pong response from server.</returns>
    Task<string> PingAsync(CancellationToken cancellationToken = default);
}
