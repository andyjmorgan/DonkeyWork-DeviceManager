using DonkeyWork.DeviceManager.DeviceClient;
using DonkeyWork.DeviceManager.DeviceClient.Services.Storage;
using DonkeyWork.DeviceManager.DeviceClient.Services.Hub;
using DonkeyWork.DeviceManager.DeviceClient.Services.Authentication;
using DonkeyWork.DeviceManager.DeviceClient.Services.Api;
using DonkeyWork.DeviceManager.DeviceClient.Services.Device;
using DonkeyWork.DeviceManager.DeviceClient.Services.OSQuery;
using Microsoft.Extensions.Options;
using Refit;
using Serilog;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true, reloadOnChange: true)
        .Build())
    .CreateLogger();

try
{
    Log.Information("Starting DonkeyWork Device Manager Client");

    var builder = Host.CreateApplicationBuilder(args);

    // Configure Serilog
    builder.Services.AddSerilog();

    // Configure and validate options
    builder.Services
        .AddOptions<DonkeyWork.DeviceManager.DeviceClient.Configuration.DeviceManagerConfiguration>()
        .BindConfiguration("DeviceManagerConfiguration")
        .ValidateDataAnnotations()
        .ValidateOnStart();

    builder.Services
        .AddOptions<DonkeyWork.DeviceManager.DeviceClient.Configuration.OSQueryConfiguration>()
        .BindConfiguration("OSQuery")
        .ValidateDataAnnotations()
        .ValidateOnStart();

    // Register services
    builder.Services.AddHttpClient();
    builder.Services.AddSingleton<ITokenStorageService, TokenStorageService>();
    builder.Services.AddSingleton<IDeviceRegistrationService, DeviceRegistrationService>();
    builder.Services.AddSingleton<ITokenRefreshService, TokenRefreshService>();
    builder.Services.AddSingleton<IDeviceHubConnectionService, DeviceHubConnectionService>();
    builder.Services.AddSingleton<IDeviceInformationDiscoveryService, DeviceInformationDiscoveryService>();

    // Register OS-specific system control service
    if (OperatingSystem.IsWindows())
    {
        builder.Services.AddSingleton<ISystemControlService, WindowsSystemControlService>();
    }
    else if (OperatingSystem.IsLinux())
    {
        builder.Services.AddSingleton<ISystemControlService, LinuxSystemControlService>();
    }
    else if (OperatingSystem.IsMacOS())
    {
        builder.Services.AddSingleton<ISystemControlService, MacOSSystemControlService>();
    }
    else
    {
        throw new PlatformNotSupportedException("System control operations are not supported on this platform");
    }

    // Register OSQuery service
    builder.Services.AddSingleton<IOSQueryService, OSQueryService>();

    // Register Refit API client with authorization
    builder.Services.AddRefitClient<IDeviceManagerApi>()
        .ConfigureHttpClient((serviceProvider, httpClient) =>
        {
            var config = serviceProvider.GetRequiredService<IOptions<DonkeyWork.DeviceManager.DeviceClient.Configuration.DeviceManagerConfiguration>>();
            httpClient.BaseAddress = new Uri(config.Value.ApiBaseUrl);
        })
        .AddHttpMessageHandler<AuthorizationMessageHandler>();

    // Register authorization handler for adding bearer tokens to requests
    builder.Services.AddTransient<AuthorizationMessageHandler>();

    // Register Worker
    builder.Services.AddHostedService<Worker>();

    // Enable running as Windows Service or Linux systemd daemon
    builder.Services.AddWindowsService(options =>
    {
        options.ServiceName = "DonkeyWork Device Manager";
    });
    builder.Services.AddSystemd();

    var host = builder.Build();

    // Check OSQuery availability at startup
    var osqueryService = host.Services.GetRequiredService<IOSQueryService>();
    var isAvailable = await osqueryService.IsAvailableAsync();
    if (!isAvailable)
    {
        Log.Warning("OSQuery is not available on this system. Query functionality will not work. Install from: https://osquery.io");
    }

    await host.RunAsync();

    Log.Information("DonkeyWork Device Manager Client stopped cleanly");
    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "DonkeyWork Device Manager Client terminated unexpectedly");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}