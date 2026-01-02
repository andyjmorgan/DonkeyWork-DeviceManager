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

    /// <summary>
    /// Gets or sets the OSQuery history.
    /// </summary>
    public DbSet<OSQueryHistoryEntity> OSQueryHistory { get; set; }

    /// <summary>
    /// Gets or sets the OSQuery executions.
    /// </summary>
    public DbSet<OSQueryExecutionEntity> OSQueryExecutions { get; set; }

    /// <summary>
    /// Gets or sets the OSQuery execution results.
    /// </summary>
    public DbSet<OSQueryExecutionResultEntity> OSQueryExecutionResults { get; set; }

    /// <summary>
    /// Gets or sets the device audit logs.
    /// </summary>
    public DbSet<DeviceAuditLogEntity> DeviceAuditLogs { get; set; }

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

        // Configure OSQuery relationships
        modelBuilder.Entity<OSQueryExecutionEntity>()
            .HasOne(x => x.QueryHistory)
            .WithMany(x => x.Executions)
            .HasForeignKey(x => x.QueryHistoryId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<OSQueryExecutionResultEntity>()
            .HasOne(x => x.Execution)
            .WithMany(x => x.Results)
            .HasForeignKey(x => x.ExecutionId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<OSQueryExecutionResultEntity>()
            .HasOne(x => x.Device)
            .WithMany()
            .HasForeignKey(x => x.DeviceId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        // Configure DeviceAuditLog relationships
        modelBuilder.Entity<DeviceAuditLogEntity>()
            .HasOne(x => x.Device)
            .WithMany()
            .HasForeignKey(x => x.DeviceId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        // Add indexes for frequently queried columns
        modelBuilder.Entity<OSQueryHistoryEntity>()
            .HasIndex(x => x.UserId);

        modelBuilder.Entity<OSQueryExecutionEntity>()
            .HasIndex(x => x.ExecutedAt);

        modelBuilder.Entity<DeviceAuditLogEntity>()
            .HasIndex(x => new { x.DeviceId, x.Timestamp });

        modelBuilder.Entity<DeviceAuditLogEntity>()
            .HasIndex(x => x.TenantId);

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