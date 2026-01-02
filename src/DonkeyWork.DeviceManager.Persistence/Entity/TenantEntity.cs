namespace DonkeyWork.DeviceManager.Persistence.Entity;

using System.ComponentModel.DataAnnotations;

public class TenantEntity
{
    /// <summary>
    /// Gets or sets the tenant identifier.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the tenant details.
    /// </summary>
    public string Name { get; set; }
    
    /// <summary>
    /// Gets or sets the initial user identifier for the tenant.
    /// </summary>
    public Guid InitialUserId { get; set; }
}