namespace DonkeyWork.DeviceManager.Api.Endpoints;

using DonkeyWork.DeviceManager.Api.Authorization;
using DonkeyWork.DeviceManager.Api.Services;
using DonkeyWork.DeviceManager.Common.Models;
using DonkeyWork.DeviceManager.Common.Models.Audit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

public static class AuditEndpoints
{
    public static IEndpointRouteBuilder MapAuditEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/audit")
            .WithTags("Audit")
            .RequireAuthorization(AuthorizationPolicies.UserOnly);

        group.MapGet("/device/{deviceId:guid}", GetDeviceAuditLogs)
            .WithName("GetDeviceAuditLogs")
            .WithSummary("Get audit logs for device")
            .WithDescription("Gets a paginated list of audit logs for a specific device")
            .Produces<PaginatedResponse<DeviceAuditLogResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError);

        group.MapGet("/recent", GetRecentAuditLogs)
            .WithName("GetRecentAuditLogs")
            .WithSummary("Get recent audit logs")
            .WithDescription("Gets a paginated list of recent audit logs for the current tenant")
            .Produces<PaginatedResponse<DeviceAuditLogResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError);

        return endpoints;
    }

    private static async Task<IResult> GetDeviceAuditLogs(
        Guid deviceId,
        IDeviceAuditService auditService,
        ILogger<IDeviceAuditService> logger,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await auditService.GetDeviceAuditLogsAsync(deviceId, page, pageSize, cancellationToken);

            logger.LogInformation("Retrieved audit logs for device {DeviceId} - page {Page} with {LogCount} logs (total: {TotalCount})",
                deviceId, result.Page, result.Items.Count, result.TotalCount);

            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error retrieving audit logs for device {DeviceId}", deviceId);
            return Results.Problem(
                title: "Failed to Retrieve Audit Logs",
                detail: "An unexpected error occurred while retrieving audit logs",
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }

    private static async Task<IResult> GetRecentAuditLogs(
        IDeviceAuditService auditService,
        ILogger<IDeviceAuditService> logger,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await auditService.GetRecentAuditLogsAsync(page, pageSize, cancellationToken);

            logger.LogInformation("Retrieved recent audit logs - page {Page} with {LogCount} logs (total: {TotalCount})",
                result.Page, result.Items.Count, result.TotalCount);

            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error retrieving recent audit logs");
            return Results.Problem(
                title: "Failed to Retrieve Audit Logs",
                detail: "An unexpected error occurred while retrieving audit logs",
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }
}
