namespace DonkeyWork.DeviceManager.Persistence.Entity;

using System.ComponentModel.DataAnnotations;

public class UserEntity
{
    /// <summary>
    /// Gets or sets the user identifier (Keycloak user ID).
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the tenant identifier.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Gets or sets the user's email address.
    /// </summary>
    public string EmailAddress { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the user's full name.
    /// </summary>
    public string FullName { get; set; } = string.Empty;
}