using DonkeyWork.DeviceManager.DeviceClient.Models;

namespace DonkeyWork.DeviceManager.DeviceClient.Services.Storage;

/// <summary>
/// Service for persisting and retrieving device authentication tokens.
/// </summary>
public interface ITokenStorageService
{
    /// <summary>
    /// Loads device tokens from local storage.
    /// </summary>
    /// <returns>The device tokens if found; otherwise, null.</returns>
    Task<DeviceTokens?> LoadTokensAsync();

    /// <summary>
    /// Saves device tokens to local storage.
    /// </summary>
    /// <param name="tokens">The device tokens to save.</param>
    Task SaveTokensAsync(DeviceTokens tokens);
}
