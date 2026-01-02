namespace DonkeyWork.DeviceManager.Persistence.Entity;

using System.ComponentModel.DataAnnotations;
using DonkeyWork.DeviceManager.Persistence.Entity.Base;

public class RoomEntity : BaseAuditEntity
{
    /// <summary>
    /// Gets or sets the room identifier.
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// Gets or sets the name of the room.
    /// </summary>
    [MaxLength(128)]
    public string Name { get; set; }
    
    /// <summary>
    /// Gets or sets the description of the room.
    /// </summary>
    [MaxLength(512)]
    public string Description { get; set; }
    
    /// <summary>
    /// Gets or sets the building this room belongs to.
    /// </summary>
    public virtual BuildingEntity Building { get; set; }

    /// <summary>
    /// Gets or sets the devices in the room.
    /// </summary>
    public virtual ICollection<DeviceEntity> Devices { get; set; } = [];
}