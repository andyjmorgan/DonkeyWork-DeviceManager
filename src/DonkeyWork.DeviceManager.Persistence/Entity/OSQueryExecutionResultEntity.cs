namespace DonkeyWork.DeviceManager.Persistence.Entity;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

/// <summary>
/// Represents the result of an OSQuery execution on a single device.
/// </summary>
public class OSQueryExecutionResultEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for this result.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the execution ID this result belongs to.
    /// </summary>
    public Guid ExecutionId { get; set; }

    /// <summary>
    /// Gets or sets the execution this result belongs to.
    /// </summary>
    public virtual OSQueryExecutionEntity Execution { get; set; } = null!;

    /// <summary>
    /// Gets or sets the device ID this result is from.
    /// </summary>
    public Guid DeviceId { get; set; }

    /// <summary>
    /// Gets or sets the device this result is from.
    /// </summary>
    public virtual DeviceEntity Device { get; set; } = null!;

    /// <summary>
    /// Gets or sets whether the query executed successfully.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if the query failed.
    /// </summary>
    [MaxLength(2000)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the raw JSON result from osquery.
    /// Stored as TEXT to support large result sets.
    /// </summary>
    [Column(TypeName = "TEXT")]
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
