namespace DonkeyWork.DeviceManager.Common.Models.OSQuery;

/// <summary>
/// Response model for a single device OSQuery execution result.
/// </summary>
public class OSQueryExecutionResultResponse
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the execution ID this result belongs to.
    /// </summary>
    public Guid ExecutionId { get; set; }

    /// <summary>
    /// Gets or sets the device ID this result is from.
    /// </summary>
    public Guid DeviceId { get; set; }

    /// <summary>
    /// Gets or sets the device name.
    /// </summary>
    public string DeviceName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the query executed successfully.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if the query failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the raw JSON result from osquery.
    /// </summary>
    public string? RawJson { get; set; }

    /// <summary>
    /// Gets or sets the execution time in milliseconds.
    /// </summary>
    public int ExecutionTimeMs { get; set; }

    /// <summary>
    /// Gets or sets the number of rows returned.
    /// </summary>
    public int RowCount { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when this result was received.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
}
