using DonkeyWork.DeviceManager.Api.Authorization;
using DonkeyWork.DeviceManager.Api.Services;
using DonkeyWork.DeviceManager.Common.Models.Provisioning;

namespace DonkeyWork.DeviceManager.Api.Endpoints;

public static class ProvisioningEndpoints
{
    public static IEndpointRouteBuilder MapProvisioningEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/provisioning")
            .WithTags("Provisioning")
            .RequireAuthorization(AuthorizationPolicies.UserOnly);

        group.MapPost("/seed-organization", SeedOrganization)
            .WithName("SeedOrganization")
            .WithSummary("Seed organizational structure")
            .WithDescription("Creates default building and room if they don't exist for the tenant. Idempotent - safe to call multiple times.")
            .Produces<ProvisionOrganizationResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError);

        return endpoints;
    }

    private static async Task<IResult> SeedOrganization(
        IOrganizationService organizationService,
        ILogger<IOrganizationService> logger)
    {
        try
        {
            var result = await organizationService.EnsureOrganizationStructureAsync();
            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error seeding organization structure");
            return Results.Problem("An error occurred while seeding the organization structure");
        }
    }
}
