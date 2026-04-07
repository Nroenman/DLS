using AirportSystem.Flights.Data;
using AirportSystem.Flights.GraphQL;
using AirportSystem.Flights.GraphQL.Types;
using AirportSystem.Flights.Services.Auth;
using AirportSystem.Flights.Services.Flights;
using AirportSystem.Flights.Services.Gates;
using AirportSystem.Flights.Services.Messaging;
using HotChocolate.Execution.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace AirportSystem.Flights.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
        return services;
    }

    public static IServiceCollection AddKeycloakAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var baseUrl = configuration["Keycloak:BaseUrl"]!;
        var realm   = configuration["Keycloak:Realm"]!;

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // Keycloak exposes an OIDC discovery document at this URL.
                // The middleware downloads it automatically and uses the
                // public key set (JWKS) it contains to verify tokens —
                // so there is no shared secret to manage in this service.
                options.Authority = $"{baseUrl}/realms/{realm}";

                // Tokens are issued for the "account" audience by default.
                // Set to false so callers using a custom frontend client id
                // are also accepted.
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer           = true,
                    ValidIssuer              = $"{baseUrl}/realms/{realm}",
                    ValidateAudience         = false,
                    ValidateLifetime         = true,
                    ClockSkew                = TimeSpan.Zero,
                    NameClaimType            = "preferred_username",
                    // Role mapping is handled by KeycloakClaimsTransformer
                };

                // Disable HTTPS requirement for local development.
                // In production, Keycloak should always be behind TLS.
                options.RequireHttpsMetadata = false;

                // Support JWT via WebSocket query-string for subscriptions
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = ctx =>
                    {
                        var token = ctx.Request.Query["access_token"].ToString();
                        if (!string.IsNullOrEmpty(token) &&
                            ctx.HttpContext.Request.Path.StartsWithSegments("/graphql"))
                        {
                            ctx.Token = token;
                        }
                        return Task.CompletedTask;
                    }
                };
            });

        // Unpack Keycloak's realm_access.roles into ClaimTypes.Role
        services.AddSingleton<IClaimsTransformation, KeycloakClaimsTransformer>();

        services.AddAuthorization();
        return services;
    }

    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services)
    {
        services.AddSingleton<IFlightEventPublisher, RabbitMqFlightEventPublisher>();
        services.AddScoped<IUserSyncService, UserSyncService>();
        services.AddScoped<IFlightService, FlightService>();
        services.AddScoped<IGateService, GateService>();

        // Typed HttpClient for Keycloak REST calls
        services.AddHttpClient<IKeycloakService, KeycloakService>();

        return services;
    }

    public static IRequestExecutorBuilder AddGraphQLConfiguration(
        this IServiceCollection services)
    {
        return services
            .AddGraphQLServer()
            .AddQueryType<Query>()
            .AddMutationType<Mutation>()
            .AddSubscriptionType<Subscription>()
            .AddType<UserType>()
            .AddType<FlightType>()
            .AddType<GateType>()
            .AddFiltering()
            .AddSorting()
            .AddInMemorySubscriptions()
            .AddAuthorization()
            .AddErrorFilter<GraphQLErrorFilter>()
            .ModifyRequestOptions(opt => opt.IncludeExceptionDetails = false);
    }
}
