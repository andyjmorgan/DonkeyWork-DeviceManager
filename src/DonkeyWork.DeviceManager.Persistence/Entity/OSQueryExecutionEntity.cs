namespace DonkeyWork.DeviceManager.Persistence.Entity;

using System.ComponentModel.DataAnnotations;
using DonkeyWork.DeviceManager.Persistence.Entity.Base;

/// <summary>
/// Represents a single execution instance of an OSQuery across one or more devices.
/// </summary>
public class OSQueryExecutionEntity : BaseAuditEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for this execution.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the ID of the query from history (nullable if ad-hoc query).
    /// </summary>
    public Guid? QueryHistoryId { get; set; }

    /// <summary>
    /// Gets or sets the query history this execution belongs to.
    /// </summary>
    public virtual OSQueryHistoryEntity? QueryHistory { get; set; }

    /// <summary>
    /// Gets or sets the SQL query that was executed.
    /// </summary>
    [MaxLength(4000)]
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
    /// Gets or sets the individual device results for this execution.
    /// </summary>
    public virtual ICollection<OSQueryExecutionResultEntity> Results { get; set; } = new List<OSQueryExecutionResultEntity>();
}
