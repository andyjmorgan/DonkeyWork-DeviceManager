namespace DonkeyWork.DeviceManager.Api.Endpoints;

using DonkeyWork.DeviceManager.Backend.Common.RequestContextServices;
using DonkeyWork.DeviceManager.Api.Authorization;
using DonkeyWork.DeviceManager.Api.DTOs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

public static class ProfileEndpoints
{
    public static IEndpointRouteBuilder MapProfileEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/profile")
            .WithTags("Profile")
            .RequireAuthorization(AuthorizationPolicies.UserOnly); // Only users can access profile endpoints

        group.MapGet("/me", GetCurrentUserProfile)
            .WithName("GetCurrentUserProfile")
            .WithSummary("Get current user's profile")
            .WithDescription("Returns the authenticated user's profile information including name, email, user ID, and tenant ID")
            .Produces<UserProfileDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        return endpoints;
    }

    private static IResult GetCurrentUserProfile(
        HttpContext context,
        IRequestContextProvider requestContextProvider)
    {
        // Extract claims from JWT (already validated by authentication middleware)
        var subClaim = context.User.FindFirst("sub")?.Value;
        var nameClaim = context.User.FindFirst("name")?.Value ?? "Unknown";
        var emailClaim = context.User.FindFirst("email")?.Value ?? "unknown@example.com";
        var tenantIdClaim = context.User.FindFirst("tenantId")?.Value;

        // Validate required claims
        if (string.IsNullOrEmpty(subClaim) || !Guid.TryParse(subClaim, out var userId))
        {
            return Results.Problem(
                title: "Invalid Token",
                detail: "User ID not found in token",
                statusCode: StatusCodes.Status401Unauthorized
            );
        }

        if (string.IsNullOrEmpty(tenantIdClaim) || !Guid.TryParse(tenantIdClaim, out var tenantId))
        {
            return Results.Problem(
                title: "Invalid Token",
                detail: "Tenant ID not found in token. User may need to re-authenticate.",
                statusCode: StatusCodes.Status401Unauthorized
            );
        }

        // Create and return profile DTO
        var profile = new UserProfileDto
        {
            UserId = userId,
            TenantId = tenantId,
            Name = nameClaim,
            Email = emailClaim
        };

        return Results.Ok(profile);
    }
}
