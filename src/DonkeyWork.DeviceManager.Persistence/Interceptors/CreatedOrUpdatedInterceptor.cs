namespace DonkeyWork.DeviceManager.Persistence.Interceptors;

using DonkeyWork.DeviceManager.Backend.Common.RequestContextServices;
using DonkeyWork.DeviceManager.Persistence.Entity.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

/// <inheritdoc />
public class CreatedOrUpdatedInterceptor(IRequestContextProvider requestContextProvider) : SaveChangesInterceptor
{
    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (eventData.Context is null)
        {
            return result;
        }

        this.UpdateEntityCreatedOrUpdated(eventData);

        return result;
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null)
        {
            return new ValueTask<InterceptionResult<int>>(result);
        }

        this.UpdateEntityCreatedOrUpdated(eventData);

        return new ValueTask<InterceptionResult<int>>(result);
    }

    private void UpdateEntityCreatedOrUpdated(DbContextEventData eventData)
    {
        // ReSharper disable once NullableWarningSuppressionIsUsed // Reason: false positive
        foreach (var entry in eventData.Context!
                     .ChangeTracker.Entries().Where(
                         entry => entry is
                         {
                             State: EntityState.Modified or EntityState.Added,
                             Entity: BaseAuditEntity
                         }))
        {
            BaseAuditEntity entity = (BaseAuditEntity)entry.Entity;
            if (entry.State is EntityState.Added)
            {
                if (!requestContextProvider.Context.IsDeviceSession)
                {
                    entity.TenantId = requestContextProvider.Context.TenantId;
                    entity.UserId = requestContextProvider.Context.UserId;
                }
            }

            entity.Updated = DateTimeOffset.UtcNow;
        }
    }
}
