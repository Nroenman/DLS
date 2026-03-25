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

app.MapReverseProxy();

app.Run();
