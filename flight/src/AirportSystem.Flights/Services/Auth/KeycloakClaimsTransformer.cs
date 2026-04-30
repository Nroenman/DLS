using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;

namespace AirportSystem.Flights.Services.Auth;

/// <summary>
/// Keycloak stores realm roles in the token as:
///   "realm_access": { "roles": ["Admin", "Staff", "Passenger", ...] }
///
/// This transformer unpacks that JSON blob and emits a standard
/// ClaimTypes.Role claim for each role so [Authorize(Roles = "Admin")]
/// works without any other changes to the rest of the codebase.
/// </summary>
public class KeycloakClaimsTransformer : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        // Only run once — avoid double-adding if the middleware fires twice
        if (principal.HasClaim(c => c.Type == ClaimTypes.Role))
            return Task.FromResult(principal);

        var identity = (ClaimsIdentity)principal.Identity!;

        var realmAccessClaim = identity.FindFirst("realm_access");
        if (realmAccessClaim is null)
            return Task.FromResult(principal);

        try
        {
            using var doc = JsonDocument.Parse(realmAccessClaim.Value);
            if (!doc.RootElement.TryGetProperty("roles", out var rolesElement))
                return Task.FromResult(principal);

            foreach (var role in rolesElement.EnumerateArray())
            {
                var roleName = role.GetString();
                if (!string.IsNullOrWhiteSpace(roleName))
                    identity.AddClaim(new Claim(ClaimTypes.Role, roleName));
            }
        }
        catch (JsonException)
        {
            // Malformed claim — ignore and continue without roles
        }

        return Task.FromResult(principal);
    }
}
