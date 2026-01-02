namespace DonkeyWork.DeviceManager.Api.Services;

using DonkeyWork.DeviceManager.Backend.Common.RequestContextServices;
using DonkeyWork.DeviceManager.Persistence.Context;
using DonkeyWork.DeviceManager.Persistence.Entity;
using Microsoft.Extensions.Logging;

public class TenantProvisioningService : ITenantProvisioningService
{
    private readonly DeviceManagerContext _dbContext;
    private readonly IRequestContextProvider _requestContextProvider;
    private readonly ILogger<TenantProvisioningService> _logger;

    public TenantProvisioningService(
        DeviceManagerContext dbContext,
        IRequestContextProvider requestContextProvider,
        ILogger<TenantProvisioningService> logger)
    {
        _dbContext = dbContext;
        _requestContextProvider = requestContextProvider;
        _logger = logger;
    }

    public async Task ProvisionTenantAsync(Guid tenantId, string keycloakUserId, string email, string fullName, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting async tenant provisioning for TenantId: {TenantId}, KeycloakUserId: {KeycloakUserId}",
            tenantId, keycloakUserId);

        var keycloakUserGuid = Guid.Parse(keycloakUserId);

        // Create tenant in database
        var tenant = new TenantEntity
        {
            Id = tenantId,
            Name = $"Tenant-{tenantId}", // Default name, can be updated later
            InitialUserId = keycloakUserGuid
        };

        _dbContext.Tenants.Add(tenant);
        _logger.LogInformation("Created tenant {TenantId} in database", tenantId);

        // Create initial user in database (Id is the Keycloak user ID)
        var user = new UserEntity
        {
            Id = keycloakUserGuid,
            TenantId = tenantId,
            EmailAddress = email,
            FullName = fullName
        };

        _dbContext.Users.Add(user);
        _logger.LogInformation("Created user {UserId} (Keycloak ID) for tenant {TenantId}",
            user.Id, tenantId);

        // Update RequestContext so the interceptor will use correct TenantId and UserId
        // for subsequently created entities
        _requestContextProvider.Context.TenantId = tenantId;
        _requestContextProvider.Context.UserId = user.Id;
        _requestContextProvider.Context.RequestId = Guid.NewGuid();
        _logger.LogInformation("Updated RequestContext - TenantId: {TenantId}, UserId: {UserId}",
            tenantId, user.Id);

        // Create default building - interceptor will set TenantId and UserId automatically
        var defaultBuilding = new BuildingEntity
        {
            Id = Guid.NewGuid(),
            Name = "Default Building",
            Description = "Default building created during tenant provisioning"
        };

        _dbContext.Buildings.Add(defaultBuilding);
        _logger.LogInformation("Created default building {BuildingId} for tenant {TenantId}",
            defaultBuilding.Id, tenantId);

        // Create default room - interceptor will set TenantId and UserId automatically
        var defaultRoom = new RoomEntity
        {
            Id = Guid.NewGuid(),
            Name = "Default Room",
            Description = "Default room created during tenant provisioning",
            Building = defaultBuilding
        };

        _dbContext.Rooms.Add(defaultRoom);
        _logger.LogInformation("Created default room {RoomId} in building {BuildingId} for tenant {TenantId}",
            defaultRoom.Id, defaultBuilding.Id, tenantId);

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Tenant, user, building, and room provisioning completed for TenantId: {TenantId}", tenantId);

        // TODO: Additional provisioning logic could include:
        // - Creating default tenant settings
        // - Creating default roles/permissions
        // - Sending welcome email
        // - etc.
    }
}
