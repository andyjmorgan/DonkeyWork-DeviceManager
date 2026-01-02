namespace DonkeyWork.DeviceManager.Common.Models.OSQuery;

/// <summary>
/// Response model for OSQuery execution with results.
/// </summary>
public class OSQueryExecutionResponse
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the ID of the query from history (null if ad-hoc query).
    /// </summary>
    public Guid? QueryHistoryId { get; set; }

    /// <summary>
    /// Gets or sets the SQL query that was executed.
    /// </summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when this execution was initiated.
    /// </summary>
    public DateTimeOffset ExecutedAt { get; set; }

    /// <summary>
    /// Gets or sets the number of devices this query was executed on.
    /// </summary>
    public int DeviceCount { get; set; }

    /// <summary>
    /// Gets or sets the number of devices that successfully returned results.
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// Gets or sets the number of devices that failed to execute the query.
    /// </summary>
    public int FailureCount { get; set; }

    /// <summary>
    /// Gets or sets the individual device results.
    /// </summary>
    public List<OSQueryExecutionResultResponse> Results { get; set; } = new();

    /// <summary>
    /// Gets or sets the user ID who executed the query.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTimeOffset Created { get; set; }
}
