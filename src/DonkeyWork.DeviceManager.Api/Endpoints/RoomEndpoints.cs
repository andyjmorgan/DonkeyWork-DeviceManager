namespace DonkeyWork.DeviceManager.Api.Endpoints;

using DonkeyWork.DeviceManager.Api.Authorization;
using DonkeyWork.DeviceManager.Api.Services;
using DonkeyWork.DeviceManager.Common.Models;
using DonkeyWork.DeviceManager.Common.Models.Room;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

public static class RoomEndpoints
{
    public static IEndpointRouteBuilder MapRoomEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/rooms")
            .WithTags("Rooms")
            .RequireAuthorization(AuthorizationPolicies.UserOnly);

        group.MapGet("", GetAllRooms)
            .WithName("GetAllRooms")
            .WithSummary("Get all rooms")
            .WithDescription("Gets all rooms for the current tenant, optionally filtered by building")
            .Produces<List<RoomResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapGet("{id:guid}", GetRoomById)
            .WithName("GetRoomById")
            .WithSummary("Get room by ID")
            .WithDescription("Gets a room with its building and devices")
            .Produces<RoomDetailsResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("", CreateRoom)
            .WithName("CreateRoom")
            .WithSummary("Create a new room")
            .WithDescription("Creates a new room in a building")
            .Produces<RoomResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPut("{id:guid}", UpdateRoom)
            .WithName("UpdateRoom")
            .WithSummary("Update a room")
            .WithDescription("Updates an existing room")
            .Produces<RoomResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("{id:guid}", DeleteRoom)
            .WithName("DeleteRoom")
            .WithSummary("Delete a room")
            .WithDescription("Deletes a room")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status400BadRequest);

        return endpoints;
    }

    private static async Task<IResult> GetAllRooms(
        IOrganizationService organizationService,
        ILogger<IOrganizationService> logger,
        Guid? buildingId = null)
    {
        try
        {
            var rooms = await organizationService.GetRoomsAsync(buildingId);
            logger.LogInformation("Retrieved {RoomCount} rooms", rooms.Count);
            return Results.Ok(rooms);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error retrieving rooms");
            return Results.Problem(
                title: "Failed to Retrieve Rooms",
                detail: "An unexpected error occurred while retrieving rooms",
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }

    private static async Task<IResult> GetRoomById(
        Guid id,
        IOrganizationService organizationService,
        ILogger<IOrganizationService> logger)
    {
        try
        {
            var room = await organizationService.GetRoomByIdAsync(id);
            if (room == null)
            {
                return Results.NotFound(new { error = "Room not found" });
            }
            return Results.Ok(room);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error retrieving room {RoomId}", id);
            return Results.Problem(
                title: "Failed to Retrieve Room",
                detail: "An unexpected error occurred while retrieving the room",
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }

    private static async Task<IResult> CreateRoom(
        CreateRoomRequest request,
        IOrganizationService organizationService,
        ILogger<IOrganizationService> logger)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Results.BadRequest(new { error = "Room name is required" });
        }

        if (request.BuildingId == Guid.Empty)
        {
            return Results.BadRequest(new { error = "Building ID is required" });
        }

        try
        {
            var room = await organizationService.CreateRoomAsync(request);
            logger.LogInformation("Room created successfully - ID: {RoomId}, Name: {Name}, BuildingId: {BuildingId}",
                room.Id, room.Name, room.BuildingId);
            return Results.Created($"/api/rooms/{room.Id}", room);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Cannot create room");
            return Results.NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error creating room");
            return Results.Problem(
                title: "Failed to Create Room",
                detail: "An unexpected error occurred while creating the room",
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }

    private static async Task<IResult> UpdateRoom(
        Guid id,
        UpdateRoomRequest request,
        IOrganizationService organizationService,
        ILogger<IOrganizationService> logger)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Results.BadRequest(new { error = "Room name is required" });
        }

        try
        {
            var room = await organizationService.UpdateRoomAsync(id, request);
            if (room == null)
            {
                return Results.NotFound(new { error = "Room not found" });
            }
            logger.LogInformation("Room updated successfully - ID: {RoomId}", id);
            return Results.Ok(room);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Invalid operation updating room");
            return Results.NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error updating room {RoomId}", id);
            return Results.Problem(
                title: "Failed to Update Room",
                detail: "An unexpected error occurred while updating the room",
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }

    private static async Task<IResult> DeleteRoom(
        Guid id,
        IOrganizationService organizationService,
        ILogger<IOrganizationService> logger)
    {
        try
        {
            var success = await organizationService.DeleteRoomAsync(id);
            if (!success)
            {
                var room = await organizationService.GetRoomByIdAsync(id);
                if (room == null)
                {
                    return Results.NotFound(new { error = "Room not found" });
                }
                return Results.BadRequest(new { error = "Cannot delete room that has devices" });
            }
            logger.LogInformation("Room deleted successfully - ID: {RoomId}", id);
            return Results.NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error deleting room {RoomId}", id);
            return Results.Problem(
                title: "Failed to Delete Room",
                detail: "An unexpected error occurred while deleting the room",
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }
}
