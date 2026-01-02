namespace DonkeyWork.DeviceManager.Persistence.Entity.Base;

public class BaseAuditEntity
{
    public Guid UserId { get; set; }
    
    public Guid TenantId { get; set; }
    
    public DateTimeOffset Created { get; init; } = DateTimeOffset.UtcNow;
    
    public DateTimeOffset Updated { get; set; }
}