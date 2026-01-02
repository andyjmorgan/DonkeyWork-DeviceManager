using System.Text.Json.Serialization;
using DonkeyWork.DeviceManager.Api.Endpoints;
using DonkeyWork.DeviceManager.Api.Extensions;
using DonkeyWork.DeviceManager.Persistence.Extensions;
using DonkeyWork.DeviceManager.Persistence.Services;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console());

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

// Configure JSON serialization to handle enums as strings
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddIdentityServices(builder.Configuration)
    .AddPersistenceServices(builder.Configuration)
    .AddCommonWebServices()
    .AddHubServices();

// Register hub filter
builder.Services.AddSingleton<DonkeyWork.DeviceManager.Api.Filters.RequestContextHubFilter>();

// Register custom user ID provider for SignalR
builder.Services.AddSingleton<Microsoft.AspNetCore.SignalR.IUserIdProvider, DonkeyWork.DeviceManager.Api.SignalR.RequestContextUserIdProvider>();

// Configure SignalR with Redis backplane
var redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
builder.Services.AddSignalR()
    .AddStackExchangeRedis(redisConnectionString, options =>
    {
        options.Configuration.ChannelPrefix = StackExchange.Redis.RedisChannel.Literal("DeviceManager");
    });

var app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference();
app.MapHealthChecks("/healthz");

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapAuthorizationEndpoints();
app.MapProfileEndpoints();
app.MapDeviceRegistrationEndpoints();
app.MapDeviceEndpoints();
app.MapDeviceInformationEndpoints();

app.MapHubEndpoints();

await using (var scope = app.Services.CreateAsyncScope())
{
    var migrationService = scope.ServiceProvider.GetRequiredService<IMigrationService>();
    await migrationService.MigrateAsync();
}

await app.RunAsync();