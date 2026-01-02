namespace DonkeyWork.DeviceManager.Api.Endpoints;

using DonkeyWork.DeviceManager.Api.Authorization;
using DonkeyWork.DeviceManager.Api.Services;
using DonkeyWork.DeviceManager.Common.Models;
using DonkeyWork.DeviceManager.Common.Models.Building;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

public static class BuildingEndpoints
{
    public static IEndpointRouteBuilder MapBuildingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/buildings")
            .WithTags("Buildings")
            .RequireAuthorization(AuthorizationPolicies.UserOnly);

        group.MapGet("", GetAllBuildings)
            .WithName("GetAllBuildings")
            .WithSummary("Get all buildings")
            .WithDescription("Gets all buildings for the current tenant")
            .Produces<List<BuildingResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapGet("{id:guid}", GetBuildingById)
            .WithName("GetBuildingById")
            .WithSummary("Get building by ID")
            .WithDescription("Gets a building with its rooms")
            .Produces<BuildingDetailsResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("", CreateBuilding)
            .WithName("CreateBuilding")
            .WithSummary("Create a new building")
            .WithDescription("Creates a new building for the current tenant")
            .Produces<BuildingResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPut("{id:guid}", UpdateBuilding)
            .WithName("UpdateBuilding")
            .WithSummary("Update a building")
            .WithDescription("Updates an existing building")
            .Produces<BuildingResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("{id:guid}", DeleteBuilding)
            .WithName("DeleteBuilding")
            .WithSummary("Delete a building")
            .WithDescription("Deletes a building and all its rooms")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status400BadRequest);

        return endpoints;
    }

    private static async Task<IResult> GetAllBuildings(
        IOrganizationService organizationService,
        ILogger<IOrganizationService> logger)
    {
        try
        {
            var buildings = await organizationService.GetBuildingsAsync();
            logger.LogInformation("Retrieved {BuildingCount} buildings", buildings.Count);
            return Results.Ok(buildings);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error retrieving buildings");
            return Results.Problem(
                title: "Failed to Retrieve Buildings",
                detail: "An unexpected error occurred while retrieving buildings",
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }

    private static async Task<IResult> GetBuildingById(
        Guid id,
        IOrganizationService organizationService,
        ILogger<IOrganizationService> logger)
    {
        try
        {
            var building = await organizationService.GetBuildingByIdAsync(id);
            if (building == null)
            {
                return Results.NotFound(new { error = "Building not found" });
            }
            return Results.Ok(building);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error retrieving building {BuildingId}", id);
            return Results.Problem(
                title: "Failed to Retrieve Building",
                detail: "An unexpected error occurred while retrieving the building",
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }

    private static async Task<IResult> CreateBuilding(
        CreateBuildingRequest request,
        IOrganizationService organizationService,
        ILogger<IOrganizationService> logger)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Results.BadRequest(new { error = "Building name is required" });
        }

        try
        {
            var building = await organizationService.CreateBuildingAsync(request);
            logger.LogInformation("Building created successfully - ID: {BuildingId}, Name: {Name}",
                building.Id, building.Name);
            return Results.Created($"/api/buildings/{building.Id}", building);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error creating building");
            return Results.Problem(
                title: "Failed to Create Building",
                detail: "An unexpected error occurred while creating the building",
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }

    private static async Task<IResult> UpdateBuilding(
        Guid id,
        UpdateBuildingRequest request,
        IOrganizationService organizationService,
        ILogger<IOrganizationService> logger)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Results.BadRequest(new { error = "Building name is required" });
        }

        try
        {
            var building = await organizationService.UpdateBuildingAsync(id, request);
            if (building == null)
            {
                return Results.NotFound(new { error = "Building not found" });
            }
            logger.LogInformation("Building updated successfully - ID: {BuildingId}", id);
            return Results.Ok(building);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error updating building {BuildingId}", id);
            return Results.Problem(
                title: "Failed to Update Building",
                detail: "An unexpected error occurred while updating the building",
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }

    private static async Task<IResult> DeleteBuilding(
        Guid id,
        IOrganizationService organizationService,
        ILogger<IOrganizationService> logger)
    {
        try
        {
            var success = await organizationService.DeleteBuildingAsync(id);
            if (!success)
            {
                var building = await organizationService.GetBuildingByIdAsync(id);
                if (building == null)
                {
                    return Results.NotFound(new { error = "Building not found" });
                }
                return Results.BadRequest(new { error = "Cannot delete building that has rooms" });
            }
            logger.LogInformation("Building deleted successfully - ID: {BuildingId}", id);
            return Results.NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error deleting building {BuildingId}", id);
            return Results.Problem(
                title: "Failed to Delete Building",
                detail: "An unexpected error occurred while deleting the building",
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }
}
