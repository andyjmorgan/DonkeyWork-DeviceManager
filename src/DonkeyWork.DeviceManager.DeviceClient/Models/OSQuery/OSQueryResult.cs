namespace DonkeyWork.DeviceManager.DeviceClient.Models.OSQuery;

/// <summary>
/// Represents the result of an OSQuery execution.
/// </summary>
public record OSQueryResult
{
    /// <summary>
    /// Indicates if the query executed successfully.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Error message if query failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Raw JSON output from osqueryi.
    /// </summary>
    public string? RawJson { get; init; }

    /// <summary>
    /// Parsed JSON data (array of dictionaries).
    /// Each dictionary represents a row with column names as keys.
    /// </summary>
    public List<Dictionary<string, object>>? Data { get; init; }

    /// <summary>
    /// Execution time in milliseconds.
    /// </summary>
    public int ExecutionTimeMs { get; init; }

    /// <summary>
    /// Number of rows returned.
    /// </summary>
    public int RowCount { get; init; }
}
