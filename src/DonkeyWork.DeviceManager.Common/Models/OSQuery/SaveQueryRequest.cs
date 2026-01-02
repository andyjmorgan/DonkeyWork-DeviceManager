namespace DonkeyWork.DeviceManager.Common.Models.OSQuery;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Request model for saving a query to history.
/// </summary>
public class SaveQueryRequest
{
    /// <summary>
    /// Gets or sets the SQL query to save.
    /// </summary>
    [Required]
    [MaxLength(4000)]
    public string Query { get; set; } = string.Empty;
}
