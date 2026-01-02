using System.ComponentModel.DataAnnotations;

namespace DonkeyWork.DeviceManager.DeviceClient.Configuration;

/// <summary>
/// Configuration for OSQuery integration.
/// </summary>
public class OSQueryConfiguration
{
    /// <summary>
    /// Path to osqueryi executable. If null, will search in PATH and common locations.
    /// </summary>
    public string? OSQueryPath { get; set; }

    /// <summary>
    /// Timeout for query execution in seconds. Default: 30 seconds.
    /// </summary>
    [Range(1, 300)]
    public int QueryTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Maximum number of rows to return per query. Default: 1000.
    /// </summary>
    [Range(1, 10000)]
    public int MaxRowsPerQuery { get; set; } = 1000;
}
