namespace DonkeyWork.DeviceManager.Persistence.Context;

using DonkeyWork.DeviceManager.Backend.Common.RequestContextServices;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// The device manager' context factory for design-time operations.
/// Required by EF Core tooling (dotnet ef migrations, efbundle) to create a DbContext
/// when the application is not running. This ensures EnableDynamicJson() is configured
/// for JSON column serialization, which is needed by efbundle when running migrations with seeding.
/// </summary>
public class DeviceManagerContextFactory : Microsoft.EntityFrameworkCore.Design.IDesignTimeDbContextFactory<DeviceManagerContext>
{
    /// <summary>
    /// Creates a DbContext for design-time operations (migrations, efbundle).
    /// </summary>
    /// <param name="args">The args.</param>
    /// <returns>The device manager context.</returns>
    public DeviceManagerContext CreateDbContext(string[] args)
    {
        var connectionString = "Host=localhost;Port=5432;Database=devicemanager;Username=donkeywork;Password=donkeywork;";

        // Create data source with dynamic JSON enabled
        var dataSourceBuilder = new Npgsql.NpgsqlDataSourceBuilder(connectionString);
        dataSourceBuilder.EnableDynamicJson();
        var dataSource = dataSourceBuilder.Build();

        var optionsBuilder = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<DeviceManagerContext>();
        optionsBuilder.UseNpgsql(dataSource);

        return new DeviceManagerContext(optionsBuilder.Options, new RequestContextProvider());
    }
}
