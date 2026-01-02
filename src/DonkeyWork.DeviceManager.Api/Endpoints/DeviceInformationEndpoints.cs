namespace DonkeyWork.DeviceManager.Api.Endpoints;

using DonkeyWork.DeviceManager.Api.Authorization;
using DonkeyWork.DeviceManager.Backend.Common.RequestContextServices;
using DonkeyWork.DeviceManager.Common.Models.DeviceInformation;
using DonkeyWork.DeviceManager.Persistence.Context;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public static class DeviceInformationEndpoints
{
    public static WebApplication MapDeviceInformationEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/device-information")
            .RequireAuthorization(AuthorizationPolicies.DeviceOnly);

        group.MapPost("/", PostDeviceInformation)
            .WithName("PostDeviceInformation")
            .WithSummary("Update device hardware information")
            .WithDescription("Allows a device to post its hardware and system information");

        return app;
    }

    private static async Task<IResult> PostDeviceInformation(
        [FromBody] PostDeviceInformationRequest request,
        DeviceManagerContext dbContext,
        IRequestContextProvider requestContextProvider,
        ILogger<PostDeviceInformationRequest> logger)
    {
        try
        {
            var userId = requestContextProvider.Context.UserId;

            if (userId == Guid.Empty)
            {
                logger.LogWarning("Device information post attempted without valid user ID");
                return Results.Unauthorized();
            }

            // Find the device by its ID (Keycloak user ID)
            var device = await dbContext.Devices
                .FirstOrDefaultAsync(d => d.Id == userId);

            if (device == null)
            {
                logger.LogWarning("Device not found for ID {DeviceId}", userId);
                return Results.NotFound(new { message = "Device not found" });
            }

            // Update device information
            device.Name = !string.IsNullOrWhiteSpace(request.DeviceName) ? request.DeviceName : device.Name;
            device.CpuCores = request.CpuCores;
            device.TotalMemoryBytes = request.TotalMemoryBytes;
            device.OperatingSystem = request.OperatingSystem;
            device.OSArchitecture = request.OSArchitecture;
            device.Architecture = request.Architecture;
            device.OperatingSystemVersion = request.OperatingSystemVersion;
            device.LastSeen = DateTimeOffset.UtcNow;

            await dbContext.SaveChangesAsync();

            logger.LogInformation(
                "Device information updated for device {DeviceId}: {CpuCores} cores, {MemoryGB} GB RAM, {OS} {OSVersion}",
                device.Id,
                device.CpuCores,
                device.TotalMemoryBytes / (1024.0 * 1024.0 * 1024.0),
                device.OperatingSystem,
                device.OperatingSystemVersion);

            return Results.Ok(new { message = "Device information updated successfully" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating device information");
            return Results.Problem("An error occurred while updating device information");
        }
    }
}
