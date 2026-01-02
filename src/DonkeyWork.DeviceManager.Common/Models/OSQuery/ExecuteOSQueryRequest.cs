namespace DonkeyWork.DeviceManager.Common.Models.OSQuery;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Request model for executing an OSQuery on one or more devices.
/// </summary>
public class ExecuteOSQueryRequest
{
    /// <summary>
    /// Gets or sets the SQL query text.
    /// </summary>
    [Required]
    [MaxLength(4000)]
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of device IDs to execute the query on.
    /// </summary>
    [Required]
    [MinLength(1)]
    [MaxLength(20)]
    public List<Guid> DeviceIds { get; set; } = new();

    /// <summary>
    /// Gets or sets the optional query history ID if this is from saved history.
    /// </summary>
    public Guid? QueryHistoryId { get; set; }
}
