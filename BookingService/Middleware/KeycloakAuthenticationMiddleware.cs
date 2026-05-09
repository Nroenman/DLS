using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json;

namespace BookingService.Middleware;

public static class KeycloakAuthenticationMiddleware
{
    public static IServiceCollection AddKeycloakAuthentication(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        var baseUrl = configuration["Keycloak:AuthServerUrl"];
        var realm = configuration["Keycloak:Realm"];
        var clientId = configuration["Keycloak:Resource"];
        var clientSecret = configuration["Keycloak:Credentials:Secret"];

        services.AddAuthentication("Bearer")
            .AddJwtBearer("Bearer", options =>
            {
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = false,
                    ValidateIssuer = false,
                    IssuerSigningKeyResolver = (token, securityToken, kid, parameters) =>
                    {
                        var client = new HttpClient();
                        var json = client.GetStringAsync($"{baseUrl}realms/{realm}/protocol/openid-connect/certs").Result;
                        var keys = new JsonWebKeySet(json);
                        return keys.GetSigningKeys();
                    }
                };
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var token = context.HttpContext.Request.Headers["Authorization"]
                            .ToString().Replace("Bearer ", "");

                        using var httpClient = new HttpClient();
                        var response = await httpClient.PostAsync(
                            $"{baseUrl}realms/{realm}/protocol/openid-connect/token/introspect",
                            new FormUrlEncodedContent(new Dictionary<string, string>
                            {
                                ["token"] = token,
                                ["client_id"] = clientId!,
                                ["client_secret"] = clientSecret!
                            }));

                        var json = await response.Content.ReadAsStringAsync();
                        var doc = JsonDocument.Parse(json);

                        if (!doc.RootElement.GetProperty("active").GetBoolean())
                            context.Fail("Token is no longer active");
                    }
                };
            });

        return services;
    }
}