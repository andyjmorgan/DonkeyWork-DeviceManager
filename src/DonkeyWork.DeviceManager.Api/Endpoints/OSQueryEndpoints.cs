namespace DonkeyWork.DeviceManager.Api.Endpoints;

using DonkeyWork.DeviceManager.Api.Authorization;
using DonkeyWork.DeviceManager.Api.Services;
using DonkeyWork.DeviceManager.Common.Models;
using DonkeyWork.DeviceManager.Common.Models.OSQuery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

public static class OSQueryEndpoints
{
    public static IEndpointRouteBuilder MapOSQueryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/osquery")
            .WithTags("OSQuery")
            .RequireAuthorization(AuthorizationPolicies.UserOnly);

        // Query History endpoints
        group.MapGet("/history", GetQueryHistory)
            .WithName("GetOSQueryHistory")
            .WithSummary("Get query history")
            .WithDescription("Gets a paginated list of query history for the current user")
            .Produces<PaginatedResponse<OSQueryHistoryResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError);

        group.MapGet("/history/{id:guid}", GetQueryHistoryById)
            .WithName("GetOSQueryHistoryById")
            .WithSummary("Get specific query from history")
            .WithDescription("Gets a specific query from history by ID")
            .Produces<OSQueryHistoryResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status500InternalServerError);

        group.MapPost("/history", SaveQueryToHistory)
            .WithName("SaveOSQueryToHistory")
            .WithSummary("Save query to history")
            .WithDescription("Saves or updates a query in history for the current user")
            .Produces<OSQueryHistoryResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError);

        group.MapDelete("/history/{id:guid}", DeleteQueryFromHistory)
            .WithName("DeleteOSQueryFromHistory")
            .WithSummary("Delete query from history")
            .WithDescription("Deletes a query from history")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status500InternalServerError);

        // Execution endpoints
        group.MapGet("/executions/{id:guid}", GetExecution)
            .WithName("GetOSQueryExecution")
            .WithSummary("Get execution with results")
            .WithDescription("Gets a specific execution with all device results")
            .Produces<OSQueryExecutionResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status500InternalServerError);

        group.MapGet("/executions", GetRecentExecutions)
            .WithName("GetRecentOSQueryExecutions")
            .WithSummary("Get recent executions")
            .WithDescription("Gets recent executions for the current user")
            .Produces<List<OSQueryExecutionResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError);

        return endpoints;
    }

    private static async Task<IResult> GetQueryHistory(
        IOSQueryService osqueryService,
        ILogger<IOSQueryService> logger,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await osqueryService.GetQueryHistoryAsync(page, pageSize, cancellationToken);

            logger.LogInformation("Retrieved OSQuery history page {Page} with {QueryCount} queries (total: {TotalCount})",
                result.Page, result.Items.Count, result.TotalCount);

            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error retrieving OSQuery history");
            return Results.Problem(
                title: "Failed to Retrieve Query History",
                detail: "An unexpected error occurred while retrieving query history",
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }

    private static async Task<IResult> GetQueryHistoryById(
        Guid id,
        IOSQueryService osqueryService,
        ILogger<IOSQueryService> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            var query = await osqueryService.GetQueryHistoryByIdAsync(id, cancellationToken);

            if (query == null)
            {
                logger.LogWarning("OSQuery history {QueryId} not found", id);
                return Results.NotFound(new { error = $"Query {id} not found" });
            }

            logger.LogInformation("Retrieved OSQuery history {QueryId}", id);
            return Results.Ok(query);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error retrieving OSQuery history {QueryId}", id);
            return Results.Problem(
                title: "Failed to Retrieve Query",
                detail: "An unexpected error occurred while retrieving the query",
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }

    private static async Task<IResult> SaveQueryToHistory(
        SaveQueryRequest request,
        IOSQueryService osqueryService,
        ILogger<IOSQueryService> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Query))
            {
                logger.LogWarning("Save query request received with empty query");
                return Results.BadRequest(new { error = "Query is required" });
            }

            var result = await osqueryService.SaveQueryToHistoryAsync(request.Query, cancellationToken);

            logger.LogInformation("Saved OSQuery to history: {QueryId}", result.Id);
            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error saving OSQuery to history");
            return Results.Problem(
                title: "Failed to Save Query",
                detail: "An unexpected error occurred while saving the query",
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }

    private static async Task<IResult> DeleteQueryFromHistory(
        Guid id,
        IOSQueryService osqueryService,
        ILogger<IOSQueryService> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            await osqueryService.DeleteQueryFromHistoryAsync(id, cancellationToken);

            logger.LogInformation("Deleted OSQuery history {QueryId}", id);
            return Results.NoContent();
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Failed to delete OSQuery history {QueryId}", id);
            return Results.NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error deleting OSQuery history {QueryId}", id);
            return Results.Problem(
                title: "Failed to Delete Query",
                detail: "An unexpected error occurred while deleting the query",
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }

    private static async Task<IResult> GetExecution(
        Guid id,
        IOSQueryService osqueryService,
        ILogger<IOSQueryService> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            var execution = await osqueryService.GetExecutionAsync(id, cancellationToken);

            if (execution == null)
            {
                logger.LogWarning("OSQuery execution {ExecutionId} not found", id);
                return Results.NotFound(new { error = $"Execution {id} not found" });
            }

            logger.LogInformation("Retrieved OSQuery execution {ExecutionId} with {ResultCount} results",
                id, execution.Results.Count);

            return Results.Ok(execution);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error retrieving OSQuery execution {ExecutionId}", id);
            return Results.Problem(
                title: "Failed to Retrieve Execution",
                detail: "An unexpected error occurred while retrieving the execution",
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }

    private static async Task<IResult> GetRecentExecutions(
        IOSQueryService osqueryService,
        ILogger<IOSQueryService> logger,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var executions = await osqueryService.GetRecentExecutionsAsync(limit, cancellationToken);

            logger.LogInformation("Retrieved {ExecutionCount} recent OSQuery executions", executions.Count);
            return Results.Ok(executions);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error retrieving recent OSQuery executions");
            return Results.Problem(
                title: "Failed to Retrieve Executions",
                detail: "An unexpected error occurred while retrieving executions",
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }
}
