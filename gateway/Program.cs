using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var keycloakBase = builder.Configuration["Keycloak:BaseUrl"]!;
var realm        = builder.Configuration["Keycloak:Realm"]!;

// Validate Keycloak-issued JWTs at the gateway boundary.
// Requests that include a token but have an invalid/expired one are
// rejected here before reaching any downstream service.
// Requests with no token pass through unchanged — downstream services
// enforce their own field-level authorization rules.
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Keycloak's OIDC discovery document supplies the JWKS automatically.
        options.Authority = $"{keycloakBase}/realms/{realm}";
        options.RequireHttpsMetadata = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer   = true,
            ValidIssuer      = $"{keycloakBase}/realms/{realm}",
            ValidateAudience = false,   // frontend client IDs vary; API enforces this
            ValidateLifetime = true,
            ClockSkew        = TimeSpan.Zero,
        };

        // Support WebSocket subscriptions that deliver the JWT as a
        // query-string parameter (?access_token=...) instead of a header.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var token = ctx.Request.Query["access_token"].ToString();
                if (!string.IsNullOrEmpty(token))
                    ctx.Token = token;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// Load route and cluster config from appsettings.json → "ReverseProxy" section.
// To add a new service, add an entry under Routes and Clusters there.
builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// Forward validated identity to downstream services as trusted headers.
// Downstream services must not be reachable except through this gateway.
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        // Decode the already-validated JWT directly to reliably extract
        // Keycloak-specific claims that the JWT Bearer handler may not map.
        var raw = context.Request.Headers.Authorization.ToString();
        if (raw.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var token   = new JwtSecurityToken(raw["Bearer ".Length..].Trim());
            var payload = token.Payload;

            var sub      = payload.Sub;
            var email    = payload.TryGetValue("email",              out var e) ? e as string : null;
            var username = payload.TryGetValue("preferred_username", out var u) ? u as string : null;

            var roles = Array.Empty<string>();
            if (payload.TryGetValue("realm_access", out var ra) && ra is not null)
            {
                try
                {
                    using var doc = JsonDocument.Parse(JsonSerializer.Serialize(ra));
                    if (doc.RootElement.TryGetProperty("roles", out var rolesEl))
                        roles = rolesEl.EnumerateArray()
                            .Select(r => r.GetString())
                            .Where(r => r is not null)
                            .ToArray()!;
                }
                catch (JsonException) { }
            }

            if (sub      is not null) context.Request.Headers["X-User-Id"]    = sub;
            if (email    is not null) context.Request.Headers["X-User-Email"] = email;
            if (username is not null) context.Request.Headers["X-User-Name"]  = username;
            if (roles.Length > 0)    context.Request.Headers["X-User-Roles"] = string.Join(",", roles);
        }
    }

    await next();
});

app.MapReverseProxy();

app.Run();
