using AirportSystem.Flights.Data;
using AirportSystem.Flights.GraphQL;
using AirportSystem.Flights.GraphQL.Types;
using AirportSystem.Flights.Services.Auth;
using AirportSystem.Flights.Services.Flights;
using AirportSystem.Flights.Services.Gates;
using AirportSystem.Flights.Services.Messaging;
using HotChocolate.Execution.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

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

    public static IServiceCollection AddGatewayAuthentication(
        this IServiceCollection services)
    {
        services
            .AddAuthentication(GatewayAuthenticationHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, GatewayAuthenticationHandler>(
                GatewayAuthenticationHandler.SchemeName, _ => { });

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
