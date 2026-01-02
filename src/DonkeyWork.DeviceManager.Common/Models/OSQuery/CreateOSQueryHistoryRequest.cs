namespace DonkeyWork.DeviceManager.Common.Models.OSQuery;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Request model for saving a query to history.
/// </summary>
public class CreateOSQueryHistoryRequest
{
    /// <summary>
    /// Gets or sets the SQL query text.
    /// </summary>
    [Required]
    [MaxLength(4000)]
    public string Query { get; set; } = string.Empty;
}
