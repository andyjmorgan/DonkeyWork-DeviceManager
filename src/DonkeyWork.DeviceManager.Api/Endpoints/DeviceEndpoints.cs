namespace DonkeyWork.DeviceManager.Api.Endpoints;

using DonkeyWork.DeviceManager.Common.Models;
using DonkeyWork.DeviceManager.Common.Models.Device;
using DonkeyWork.DeviceManager.Api.Authorization;
using DonkeyWork.DeviceManager.Api.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

public static class DeviceEndpoints
{
    public static IEndpointRouteBuilder MapDeviceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // Unauthenticated device operations (token management)
        var deviceGroup = endpoints.MapGroup("/api/device")
            .WithTags("Device");

        deviceGroup.MapPost("/refresh", RefreshTokenAsync)
            .WithName("RefreshDeviceToken")
            .WithSummary("Refresh device access token")
            .WithDescription("Refreshes a device's access token using its refresh token. This endpoint is called by IoT devices to maintain their authentication.")
            .Produces<RefreshTokenResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .AllowAnonymous(); // No auth required - refresh token itself is the credential

        // User-facing device management operations
        var group = endpoints.MapGroup("/api/devices")
            .WithTags("Devices")
            .RequireAuthorization(AuthorizationPolicies.UserOnly);

        group.MapGet("", GetAllDevices)
            .WithName("GetAllDevices")
            .WithSummary("Get paginated list of devices")
            .WithDescription("Gets a paginated list of devices for the current tenant with their room and building information")
            .Produces<PaginatedResponse<DeviceResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError);

        group.MapDelete("{id:guid}", DeleteDevice)
            .WithName("DeleteDevice")
            .WithSummary("Delete device")
            .WithDescription("Deletes a device from both the database and Keycloak")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status500InternalServerError);

        return endpoints;
    }

    private static async Task<IResult> GetAllDevices(
        IDeviceManagementService deviceManagementService,
        ILogger<IDeviceManagementService> logger,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await deviceManagementService.GetDevicesAsync(page, pageSize, cancellationToken);

            logger.LogInformation("Retrieved page {Page} with {DeviceCount} devices (total: {TotalCount})",
                result.Page, result.Items.Count, result.TotalCount);

            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error retrieving devices");
            return Results.Problem(
                title: "Failed to Retrieve Devices",
                detail: "An unexpected error occurred while retrieving devices",
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }

    private static async Task<IResult> DeleteDevice(
        Guid id,
        IDeviceManagementService deviceManagementService,
        ILogger<IDeviceManagementService> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            await deviceManagementService.DeleteDeviceAsync(id, cancellationToken);

            logger.LogInformation("Device {DeviceId} deleted successfully", id);

            return Results.NoContent();
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Failed to delete device {DeviceId}", id);
            return Results.NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error deleting device {DeviceId}", id);
            return Results.Problem(
                title: "Failed to Delete Device",
                detail: "An unexpected error occurred while deleting the device",
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }

    private static async Task<IResult> RefreshTokenAsync(
        RefreshTokenRequest request,
        IDeviceTokenService deviceTokenService,
        ILogger<IDeviceTokenService> logger)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            logger.LogWarning("Token refresh request received with empty refresh token");
            return Results.BadRequest(new { error = "refresh_token is required" });
        }

        logger.LogInformation("Processing device token refresh request");

        var result = await deviceTokenService.RefreshDeviceTokenAsync(request.RefreshToken);

        if (result == null)
        {
            logger.LogWarning("Token refresh failed - refresh token invalid or expired");
            return Results.Unauthorized();
        }

        var (accessToken, refreshToken, expiresIn) = result.Value;

        var response = new RefreshTokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = expiresIn
        };

        logger.LogInformation("Device token refreshed successfully. Expires in {ExpiresIn} seconds", expiresIn);

        return Results.Ok(response);
    }
}
