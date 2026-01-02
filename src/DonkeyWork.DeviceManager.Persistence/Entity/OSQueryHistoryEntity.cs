namespace DonkeyWork.DeviceManager.Persistence.Entity;

using System.ComponentModel.DataAnnotations;
using DonkeyWork.DeviceManager.Persistence.Entity.Base;

/// <summary>
/// Represents a saved OSQuery query in the user's history.
/// </summary>
public class OSQueryHistoryEntity : BaseAuditEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for this query.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the SQL query text.
    /// </summary>
    [MaxLength(4000)]
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of times this query has been executed.
    /// </summary>
    public int ExecutionCount { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the last execution.
    /// </summary>
    public DateTimeOffset? LastExecutedAt { get; set; }

    /// <summary>
    /// Gets or sets the query executions associated with this query.
    /// </summary>
    public virtual ICollection<OSQueryExecutionEntity> Executions { get; set; } = new List<OSQueryExecutionEntity>();
}
