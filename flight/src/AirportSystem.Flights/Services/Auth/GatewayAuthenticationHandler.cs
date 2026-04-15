using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace AirportSystem.Flights.Services.Auth;

/// <summary>
/// Reads the trusted identity headers forwarded by the gateway
/// (X-User-Id, X-User-Email, X-User-Name, X-User-Roles) and builds
/// a ClaimsPrincipal. No token validation is performed here — the
/// gateway is the sole entry point and is responsible for that.
/// </summary>
public class GatewayAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Gateway";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var userId = Request.Headers["X-User-Id"].FirstOrDefault();
        if (userId is null)
            return Task.FromResult(AuthenticateResult.NoResult());

        var email    = Request.Headers["X-User-Email"].FirstOrDefault();
        var username = Request.Headers["X-User-Name"].FirstOrDefault();
        var rawRoles = Request.Headers["X-User-Roles"].FirstOrDefault();
        var roles    = rawRoles?.Split(',', StringSplitOptions.RemoveEmptyEntries)
                       ?? [];

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
        };

        if (email    is not null) claims.Add(new(ClaimTypes.Email, email));
        if (username is not null) claims.Add(new(ClaimTypes.Name,  username));
        foreach (var role in roles)
            claims.Add(new(ClaimTypes.Role, role));

        var identity  = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket    = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
