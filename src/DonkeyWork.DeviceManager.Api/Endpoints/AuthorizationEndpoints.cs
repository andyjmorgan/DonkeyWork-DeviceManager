namespace DonkeyWork.DeviceManager.Api.Endpoints;

using DonkeyWork.DeviceManager.Api.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

public static class AuthorizationEndpoints
{
    public static IEndpointRouteBuilder MapAuthorizationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/auth")
            .WithTags("Authorization");

        group.MapPost("/callback", HandleAuthorizationCallback)
            .WithName("AuthorizationCallback")
            .WithSummary("Handle OAuth authorization callback from Keycloak")
            .WithDescription("Exchanges the authorization code for access tokens")
            .Produces<TokenResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
            .AllowAnonymous();

        return endpoints;
    }

    private static async Task<IResult> HandleAuthorizationCallback(
        [FromBody] AuthorizationRequest request,
        [FromServices] IUserAuthService userAuthService,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Code))
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Invalid Request",
                    Detail = "Authorization code is required",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            if (string.IsNullOrWhiteSpace(request.State))
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Invalid Request",
                    Detail = "State parameter is required",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            if (string.IsNullOrWhiteSpace(request.RedirectUri))
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Invalid Request",
                    Detail = "Redirect URI is required",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            var tokenResponse = await userAuthService.AuthorizeUserAsync(request.Code, request.State, request.RedirectUri);

            return Results.Ok(tokenResponse);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(
                title: "Authorization Failed",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest
            );
        }
        catch (Exception)
        {
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An unexpected error occurred during authorization",
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }
}

public record AuthorizationRequest(string Code, string State, string RedirectUri);
