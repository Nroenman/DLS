using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace BookingService.Middleware;

public static class KeycloakAuthenticationMiddleware
{
    public static IServiceCollection AddKeycloakAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var baseUrl = configuration["Keycloak:AuthServerUrl"]!.TrimEnd('/');
        var realm   = configuration["Keycloak:Realm"]!;

        services.AddAuthentication("Bearer")
            .AddJwtBearer("Bearer", options =>
            {
                // Authority triggers automatic JWKS discovery and caching —
                // keys are only re-fetched when an unknown kid is encountered.
                options.Authority            = $"{baseUrl}/realms/{realm}";
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = false,
                    ValidateIssuer   = false,
                };
            });

        return services;
    }
}