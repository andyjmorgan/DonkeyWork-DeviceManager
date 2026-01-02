namespace DonkeyWork.DeviceManager.Persistence.Context;

using DonkeyWork.DeviceManager.Backend.Common.RequestContextServices;
using DonkeyWork.DeviceManager.Persistence.Entity;
using DonkeyWork.DeviceManager.Persistence.Entity.Base;
using Microsoft.EntityFrameworkCore;

public class DeviceManagerContext(DbContextOptions options, IRequestContextProvider requestContextProvider)
: DbContext(options)
{
    private readonly IRequestContextProvider _requestContextProvider = requestContextProvider;

    /// <summary>
    /// Gets or sets the users.
    /// </summary>
    public DbSet<UserEntity> Users { get; set; }

    /// <summary>
    /// Gets or sets the devices.
    /// </summary>
    public DbSet<DeviceEntity> Devices { get; set; }

    /// <summary>
    /// Gets or sets the rooms.
    /// </summary>
    public DbSet<RoomEntity> Rooms { get; set; }

    /// <summary>
    /// Gets or sets the buildings.
    /// </summary>
    public DbSet<BuildingEntity> Buildings { get; set; }

    /// <summary>
    /// Gets or sets the device registrations.
    /// </summary>
    public DbSet<DeviceRegistrationEntity> DeviceRegistrations { get; set; }

    /// <summary>
    /// Gets or sets the tenants.
    /// </summary>
    public DbSet<TenantEntity> Tenants { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("DeviceManager");

        modelBuilder.Entity<UserEntity>()
            .HasIndex(x => x.Id);

        modelBuilder.Entity<DeviceEntity>()
            .HasOne(x => x.Room)
            .WithMany(x => x.Devices)
            .IsRequired();

        modelBuilder.Entity<RoomEntity>()
            .HasOne(x => x.Building)
            .WithMany(x => x.Rooms)
            .IsRequired();

        // Configure enums to be stored as strings
        modelBuilder.Entity<DeviceEntity>()
            .Property(x => x.OperatingSystem)
            .HasConversion<string>();

        modelBuilder.Entity<DeviceEntity>()
            .Property(x => x.OSArchitecture)
            .HasConversion<string>();

        modelBuilder.Entity<DeviceEntity>()
            .Property(x => x.Architecture)
            .HasConversion<string>();

        // Apply global query filters for tenant isolation
        ApplyTenantQueryFilters(modelBuilder);

        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// Applies global query filters to all entities that inherit from BaseAuditEntity
    /// to automatically filter by TenantId from the current RequestContext.
    /// </summary>
    /// <param name="modelBuilder">The model builder</param>
    private void ApplyTenantQueryFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            // Check if the entity inherits from BaseAuditEntity
            if (typeof(BaseAuditEntity).IsAssignableFrom(entityType.ClrType))
            {
                // Use reflection to call the generic SetQueryFilter method
                var method = typeof(DeviceManagerContext)
                    .GetMethod(nameof(SetTenantQueryFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.MakeGenericMethod(entityType.ClrType);

                method?.Invoke(this, new object[] { modelBuilder });
            }
        }
    }

    /// <summary>
    /// Sets the query filter for a specific entity type.
    /// This method ensures the filter is evaluated at query time, not at model build time.
    /// </summary>
    private void SetTenantQueryFilter<TEntity>(ModelBuilder modelBuilder) where TEntity : BaseAuditEntity
    {
        modelBuilder.Entity<TEntity>().HasQueryFilter(e => e.TenantId == _requestContextProvider.Context.TenantId);
    }
}