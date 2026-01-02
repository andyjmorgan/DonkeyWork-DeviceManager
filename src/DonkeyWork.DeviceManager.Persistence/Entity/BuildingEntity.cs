namespace DonkeyWork.DeviceManager.Persistence.Entity;

using System.ComponentModel.DataAnnotations;
using DonkeyWork.DeviceManager.Persistence.Entity.Base;

public class BuildingEntity : BaseAuditEntity
{
    /// <summary>
    /// Gets or sets the building identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the building.
    /// </summary>
    [MaxLength(128)]
    public string Name { get; set; }
    
    /// <summary>
    /// Gets or sets the description of the room.
    /// </summary>
    [MaxLength(512)]
    public string Description { get; set; }
    
    /// <summary>
    /// Gets or sets the rooms in the building.
    /// </summary>
    public virtual ICollection<RoomEntity> Rooms { get; set; }
    
    /// <summary>
    /// Gets or sets the devices in the building.
    /// </summary>
    public virtual ICollection<DeviceEntity> Devices { get; set; }
}