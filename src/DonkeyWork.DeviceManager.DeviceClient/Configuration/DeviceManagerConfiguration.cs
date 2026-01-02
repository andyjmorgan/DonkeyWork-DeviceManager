using System.ComponentModel.DataAnnotations;

namespace DonkeyWork.DeviceManager.DeviceClient.Configuration;

/// <summary>
/// Device Manager API configuration.
/// All device operations (registration, token refresh, etc.) go through the API.
/// </summary>
public class DeviceManagerConfiguration
{
    /// <summary>
    /// Gets or sets the Device Manager API base URL.
    /// Example: http://localhost:8787 or https://api.donkeywork.dev
    /// </summary>
    [Required]
    [Url]
    public required string ApiBaseUrl { get; set; }
}
