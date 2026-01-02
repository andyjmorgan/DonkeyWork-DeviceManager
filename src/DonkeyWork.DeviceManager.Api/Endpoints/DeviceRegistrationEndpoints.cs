namespace DonkeyWork.DeviceManager.Api.Endpoints;

using DonkeyWork.DeviceManager.Common.Models;
using DonkeyWork.DeviceManager.Common.Models.Registration;
using DonkeyWork.DeviceManager.Api.Authorization;
using DonkeyWork.DeviceManager.Api.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

public static class DeviceRegistrationEndpoints
{
    public static IEndpointRouteBuilder MapDeviceRegistrationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/device-registration")
            .WithTags("Device Registration")
            .RequireAuthorization(AuthorizationPolicies.UserOnly); // Only users can register devices

        group.MapGet("/lookup", LookupDeviceRegistration)
            .WithName("LookupDeviceRegistration")
            .WithSummary("Lookup device registration")
            .WithDescription("Looks up a device registration by three-word code and returns user's buildings and rooms")
            .Produces<DeviceRegistrationLookupResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/complete", CompleteDeviceRegistration)
            .WithName("CompleteDeviceRegistration")
            .WithSummary("Complete device registration")
            .WithDescription("Completes device registration by validating the three-word code and provisioning the device with credentials")
            .Produces<CompleteRegistrationResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static async Task<IResult> LookupDeviceRegistration(
        string code,
        IDeviceRegistrationService deviceRegistrationService,
        ILogger<IDeviceRegistrationService> logger,
        CancellationToken cancellationToken)
    {
        // Validate request
        if (string.IsNullOrWhiteSpace(code))
        {
            return Results.BadRequest(new { error = "Three-word code is required" });
        }

        try
        {
            // Lookup registration
            var lookup = await deviceRegistrationService.LookupRegistrationAsync(code, cancellationToken);

            logger.LogInformation("Device registration lookup successful - Code: {ThreeWordCode}, Buildings: {BuildingCount}",
                code, lookup.Buildings.Count);

            return Results.Ok(lookup);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Device registration lookup failed - Code: {ThreeWordCode}", code);
            return Results.NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during device registration lookup - Code: {ThreeWordCode}", code);
            return Results.Problem(
                title: "Lookup Failed",
                detail: "An unexpected error occurred during device registration lookup",
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }

    private static async Task<IResult> CompleteDeviceRegistration(
        CompleteRegistrationRequest request,
        IDeviceRegistrationService deviceRegistrationService,
        ILogger<IDeviceRegistrationService> logger,
        CancellationToken cancellationToken)
    {
        // Validate request
        if (string.IsNullOrWhiteSpace(request.ThreeWordCode))
        {
            return Results.BadRequest(new { error = "Three-word code is required" });
        }

        if (request.RoomId == Guid.Empty)
        {
            return Results.BadRequest(new { error = "Room ID is required" });
        }

        try
        {
            // Complete registration
            var credentials = await deviceRegistrationService.CompleteRegistrationAsync(
                request.ThreeWordCode,
                request.RoomId,
                cancellationToken);

            logger.LogInformation("Device registration completed successfully - Code: {ThreeWordCode}, DeviceUserId: {DeviceUserId}, TenantId: {TenantId}",
                request.ThreeWordCode, credentials.DeviceUserId, credentials.TenantId);

            // Return success response
            return Results.Ok(new CompleteRegistrationResponse
            {
                Success = true,
                Message = "Device registration completed successfully. Credentials have been sent to the device.",
                DeviceUserId = credentials.DeviceUserId
            });
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Device registration failed - Code: {ThreeWordCode}", request.ThreeWordCode);
            return Results.NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during device registration - Code: {ThreeWordCode}", request.ThreeWordCode);
            return Results.Problem(
                title: "Registration Failed",
                detail: "An unexpected error occurred during device registration",
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }
}

/// <summary>
/// Response after completing device registration.
/// </summary>
public record CompleteRegistrationResponse
{
    /// <summary>
    /// Gets whether the registration was successful.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Gets a message describing the result.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Gets the device user ID that was created.
    /// </summary>
    public required Guid DeviceUserId { get; init; }
}
