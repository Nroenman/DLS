using System.Security.Claims;
using AirportSystem.Flights.Models;
using AirportSystem.Flights.Services.Auth;
using AirportSystem.Tests.Helpers;
using FluentAssertions;

namespace AirportSystem.Tests.Services;

public class UserSyncServiceTests
{
    private static (UserSyncService service, Flights.Data.AppDbContext db) Setup()
    {
        var db      = DbContextFactory.Create();
        var service = new UserSyncService(db);
        return (service, db);
    }

    private static ClaimsPrincipal BuildPrincipal(
        Guid? sub          = null,
        string username    = "testuser",
        string email       = "test@example.com",
        string role        = "Passenger")
    {
        var id = sub ?? Guid.NewGuid();
        var identity = new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.NameIdentifier, id.ToString()),
                new Claim("preferred_username",      username),
                new Claim(ClaimTypes.Email,          email),
                new Claim(ClaimTypes.Role,           role)
            },
            "Bearer");
        return new ClaimsPrincipal(identity);
    }

    // ── First-time sync (provision) ───────────────────────────────────────────

    [Fact]
    public async Task Sync_NewUser_CreatesLocalRecord()
    {
        var (service, db) = Setup();
        var principal = BuildPrincipal(username: "alice", email: "alice@test.com");

        var user = await service.SyncAsync(principal);

        user.Should().NotBeNull();
        user.Username.Should().Be("alice");
        user.Email.Should().Be("alice@test.com");
        user.Role.Should().Be(UserRole.Passenger);

        db.Users.Should().ContainSingle(u => u.Id == user.Id);
    }

    [Fact]
    public async Task Sync_NewUser_IdMatchesKeycloakSub()
    {
        var (service, _) = Setup();
        var keycloakId   = Guid.NewGuid();
        var principal    = BuildPrincipal(sub: keycloakId);

        var user = await service.SyncAsync(principal);

        user.Id.Should().Be(keycloakId);
    }

    [Fact]
    public async Task Sync_NewAdminUser_AssignsAdminRole()
    {
        var (service, _) = Setup();
        var principal = BuildPrincipal(role: "Admin");

        var user = await service.SyncAsync(principal);

        user.Role.Should().Be(UserRole.Admin);
    }

    [Fact]
    public async Task Sync_NewStaffUser_AssignsStaffRole()
    {
        var (service, _) = Setup();
        var principal = BuildPrincipal(role: "Staff");

        var user = await service.SyncAsync(principal);

        user.Role.Should().Be(UserRole.Staff);
    }

    // ── Subsequent sync (update) ──────────────────────────────────────────────

    [Fact]
    public async Task Sync_ExistingUser_UpdatesEmailAndUsername()
    {
        var (service, db) = Setup();
        var id = Guid.NewGuid();

        // First call: provision
        await service.SyncAsync(BuildPrincipal(sub: id, username: "oldname", email: "old@test.com"));

        // Second call: Keycloak reports updated profile
        var user = await service.SyncAsync(BuildPrincipal(sub: id, username: "newname", email: "new@test.com"));

        user.Username.Should().Be("newname");
        user.Email.Should().Be("new@test.com");

        // Still only one row
        db.Users.Count(u => u.Id == id).Should().Be(1);
    }

    [Fact]
    public async Task Sync_ExistingUser_UpdatesLastSeenAt()
    {
        var (service, db) = Setup();
        var id = Guid.NewGuid();

        await service.SyncAsync(BuildPrincipal(sub: id));

        var before = db.Users.Find(id)!.LastSeenAt;
        await Task.Delay(10); // ensure time advances

        await service.SyncAsync(BuildPrincipal(sub: id));

        var after = db.Users.Find(id)!.LastSeenAt;
        after.Should().BeOnOrAfter(before);
    }

    [Fact]
    public async Task Sync_ExistingUser_RolePromotion_Persists()
    {
        var (service, _) = Setup();
        var id = Guid.NewGuid();

        await service.SyncAsync(BuildPrincipal(sub: id, role: "Passenger"));

        // User was promoted to Staff in Keycloak
        var user = await service.SyncAsync(BuildPrincipal(sub: id, role: "Staff"));

        user.Role.Should().Be(UserRole.Staff);
    }

    // ── Guard clauses ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Sync_MissingSubClaim_ThrowsUnauthorizedAccessException()
    {
        var (service, _) = Setup();
        var principal    = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.Email, "x@test.com") }, "Bearer"));

        var act = async () => await service.SyncAsync(principal);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*sub*");
    }

    [Fact]
    public async Task Sync_InvalidSubClaim_ThrowsUnauthorizedAccessException()
    {
        var (service, _) = Setup();
        var principal    = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, "not-a-guid") }, "Bearer"));

        var act = async () => await service.SyncAsync(principal);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*not a valid UUID*");
    }
}
