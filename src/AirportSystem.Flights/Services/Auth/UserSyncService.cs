using System.Security.Claims;
using AirportSystem.Flights.Data;
using AirportSystem.Flights.Models;
using Microsoft.EntityFrameworkCore;

namespace AirportSystem.Flights.Services.Auth;

public interface IUserSyncService
{
    /// <summary>
    /// Looks up the local User for the currently authenticated principal.
    /// Creates a new row if this is the first time we see this Keycloak sub,
    /// and updates Username/Email/Role on every call so they stay in sync
    /// with whatever Keycloak reports.
    /// </summary>
    Task<User> SyncAsync(ClaimsPrincipal principal);
}

public class UserSyncService : IUserSyncService
{
    private readonly AppDbContext _db;

    public UserSyncService(AppDbContext db) => _db = db;

    public async Task<User> SyncAsync(ClaimsPrincipal principal)
    {
        // sub is the stable Keycloak user UUID
        var subClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub")
            ?? throw new UnauthorizedAccessException("JWT is missing the 'sub' claim.");

        if (!Guid.TryParse(subClaim, out var keycloakId))
            throw new UnauthorizedAccessException($"JWT 'sub' claim is not a valid UUID: {subClaim}");

        var username = principal.FindFirstValue("preferred_username")
                    ?? principal.FindFirstValue(ClaimTypes.Name)
                    ?? subClaim;

        var email = principal.FindFirstValue(ClaimTypes.Email)
                 ?? principal.FindFirstValue("email")
                 ?? string.Empty;

        // Map the first matching Keycloak realm role to our enum
        var role = ResolveRole(principal);

        var user = await _db.Users.FindAsync(keycloakId);

        if (user is null)
        {
            user = new User
            {
                Id        = keycloakId,
                Username  = username,
                Email     = email,
                Role      = role,
                CreatedAt = DateTime.UtcNow,
                LastSeenAt = DateTime.UtcNow
            };
            _db.Users.Add(user);
        }
        else
        {
            // Keep local record in sync with Keycloak
            user.Username   = username;
            user.Email      = email;
            user.Role       = role;
            user.LastSeenAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return user;
    }

    private static UserRole ResolveRole(ClaimsPrincipal principal)
    {
        if (principal.IsInRole("Admin"))   return UserRole.Admin;
        if (principal.IsInRole("Staff"))   return UserRole.Staff;
        return UserRole.Passenger;
    }
}
