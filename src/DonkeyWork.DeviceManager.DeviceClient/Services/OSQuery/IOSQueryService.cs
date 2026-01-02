using DonkeyWork.DeviceManager.DeviceClient.Models.OSQuery;

namespace DonkeyWork.DeviceManager.DeviceClient.Services.OSQuery;

/// <summary>
/// Service for executing OSQuery queries on the local system.
/// </summary>
public interface IOSQueryService
{
    /// <summary>
    /// Checks if OSQuery is available on the system.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if OSQuery is available, false otherwise</returns>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes an OSQuery SQL query and returns results as JSON.
    /// </summary>
    /// <param name="query">SQL query to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Query results as parsed JSON document</returns>
    /// <exception cref="OSQueryNotAvailableException">Thrown when OSQuery is not installed</exception>
    /// <exception cref="OSQueryTimeoutException">Thrown when query execution exceeds timeout</exception>
    /// <exception cref="OSQueryException">Thrown for other OSQuery errors</exception>
    Task<OSQueryResult> ExecuteQueryAsync(string query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the path to the osqueryi executable.
    /// </summary>
    /// <returns>Path to osqueryi executable, or null if not found</returns>
    string? GetOSQueryPath();
}
