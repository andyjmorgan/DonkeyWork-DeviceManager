namespace DonkeyWork.DeviceManager.Api.Extensions;

using DonkeyWork.DeviceManager.Api.Authorization;
using DonkeyWork.DeviceManager.Api.Configuration;
using DonkeyWork.DeviceManager.Api.Hubs;
using DonkeyWork.DeviceManager.Api.Services;
using DonkeyWork.DeviceManager.Api.Services.HubActivity;
using DonkeyWork.DeviceManager.Backend.Common.RequestContextServices;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddIdentityServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure Keycloak settings
        services.AddOptions<KeycloakConfiguration>()
            .BindConfiguration(nameof(KeycloakConfiguration))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Configure JWT Bearer Authentication
        var keycloakConfig = configuration.GetSection(nameof(KeycloakConfiguration)).Get<KeycloakConfiguration>();
        if (keycloakConfig != null)
        {
            // Clear default claim type mappings to preserve original JWT claim names
            Microsoft.IdentityModel.JsonWebTokens.JsonWebTokenHandler.DefaultInboundClaimTypeMap.Clear();

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Authority = keycloakConfig.Authority;
                    options.Audience = keycloakConfig.ClientId;

                    // Use internal authority for backchannel metadata/JWKS fetching if available
                    if (!string.IsNullOrEmpty(keycloakConfig.InternalAuthority))
                    {
                        var internalMetadataAddress = $"{keycloakConfig.InternalAuthority}/.well-known/openid-configuration";
                        options.MetadataAddress = internalMetadataAddress;
                        options.RequireHttpsMetadata = false; // Internal k8s uses HTTP
                    }
                    else
                    {
                        options.RequireHttpsMetadata = true;
                    }

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = false, // Keycloak uses "account" as audience
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = keycloakConfig.Authority, // Keep public URL for issuer validation
                        ClockSkew = TimeSpan.FromMinutes(5),
                        NameClaimType = "name",
                        RoleClaimType = "roles"
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            // Allow SignalR to pass tokens via query string
                            var accessToken = context.Request.Query["access_token"];
                            var path = context.HttpContext.Request.Path;

                            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                            {
                                context.Token = accessToken;
                            }

                            return Task.CompletedTask;
                        },
                        OnTokenValidated = context =>
                        {
                            // Populate RequestContext from JWT claims
                            var requestContextProvider = context.HttpContext.RequestServices.GetRequiredService<IRequestContextProvider>();
                            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<IUserAuthService>>();
                            var success = requestContextProvider.Context.PopulateFromPrincipal(context.Principal, logger);

                            if (success)
                            {
                                logger.LogInformation("JWT validated - UserId: {UserId}, TenantId: {TenantId}, RequestId: {RequestId}, IsDevice: {IsDevice}",
                                    requestContextProvider.Context.UserId,
                                    requestContextProvider.Context.TenantId,
                                    requestContextProvider.Context.RequestId,
                                    requestContextProvider.Context.IsDeviceSession);
                            }
                            else
                            {
                                logger.LogWarning("Failed to populate RequestContext from JWT claims");
                            }

                            return Task.CompletedTask;
                        },
                        OnAuthenticationFailed = context =>
                        {
                            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<IUserAuthService>>();
                            logger.LogError(context.Exception, "JWT authentication failed");
                            return Task.CompletedTask;
                        }
                    };
                });

            // Register authorization handlers as scoped (they depend on scoped IRequestContextProvider)
            services.AddScoped<IAuthorizationHandler, UserOnlyAuthorizationHandler>();
            services.AddScoped<IAuthorizationHandler, DeviceOnlyAuthorizationHandler>();

            // Configure authorization policies
            services.AddAuthorization(options =>
            {
                // Policy that only allows user connections (not devices)
                options.AddPolicy(AuthorizationPolicies.UserOnly, policy =>
                    policy.Requirements.Add(new UserOnlyRequirement()));

                // Policy that only allows device connections (not users)
                options.AddPolicy(AuthorizationPolicies.DeviceOnly, policy =>
                    policy.Requirements.Add(new DeviceOnlyRequirement()));
            });
        }

        // Register HTTP client for Keycloak communication
        services.AddHttpClient();

        // Register authentication service
        services.AddScoped<IUserAuthService, UserAuthService>();

        // Register tenant provisioning service
        services.AddScoped<ITenantProvisioningService, TenantProvisioningService>();

        // Register device registration service
        services.AddScoped<IDeviceRegistrationService, DeviceRegistrationService>();

        // Register device management service
        services.AddScoped<IDeviceManagementService, DeviceManagementService>();

        // Register device token service for token refresh
        services.AddScoped<IDeviceTokenService, DeviceTokenService>();

        return services;
    }

    public static IServiceCollection AddCommonWebServices(this IServiceCollection services)
    {
        // Register RequestContext as scoped (per HTTP request)
        services.AddScoped<IRequestContextProvider, RequestContextProvider>();

        return services;
    }

    public static IServiceCollection AddHubServices(this IServiceCollection services)
    {
        // Register three-word code generator for device registration
        services.AddSingleton<ThreeWordCodeGenerator>();

        // Register hub activity channel and processor
        services.AddSingleton<IHubActivityChannel, HubActivityChannel>();
        services.AddHostedService<HubActivityProcessor>();

        return services;
    }

    public static WebApplication MapHubEndpoints(this WebApplication app)
    {
        app.MapHub<DeviceHub>("/hubs/device");
        app.MapHub<DeviceRegistrationHub>("/hubs/device-registration");
        app.MapHub<UserHub>("/hubs/user");

        return app;
    }
}
