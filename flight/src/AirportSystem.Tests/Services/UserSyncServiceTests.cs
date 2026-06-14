using System.Security.Claims;
using System.Text.Encodings.Web;
using AirportSystem.Flights.Extensions;
using AirportSystem.Flights.Models;
using AirportSystem.Flights.Services.Auth;
using AirportSystem.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace AirportSystem.Tests.Services;

public class UserSyncServiceTests
{
    private static (UserSyncService service, Flights.Data.AppDbContext db) Setup()
    {
        var db = DbContextFactory.Create();
        var service = new UserSyncService(db);
        return (service, db);
    }

    private static ClaimsPrincipal BuildPrincipal(
        Guid? sub = null,
        string username = "testuser",
        string email = "test@example.com",
        string role = "Passenger")
    {
        var id = sub ?? Guid.NewGuid();
        var identity = new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.NameIdentifier, id.ToString()),
                new Claim("preferred_username", username),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, role)
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
        var keycloakId = Guid.NewGuid();
        var principal = BuildPrincipal(sub: keycloakId);

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
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.Email, "x@test.com") }, "Bearer"));

        var act = async () => await service.SyncAsync(principal);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*sub*");
    }

    [Fact]
    public async Task Sync_InvalidSubClaim_ThrowsUnauthorizedAccessException()
    {
        var (service, _) = Setup();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, "not-a-guid") }, "Bearer"));

        var act = async () => await service.SyncAsync(principal);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*not a valid UUID*");
    }

    public class GatewayAuthenticationHandlerTests
    {
        private static async Task<(GatewayAuthenticationHandler handler, DefaultHttpContext ctx)>
            BuildHandlerAsync()
        {
            var options = new Mock<IOptionsMonitor<AuthenticationSchemeOptions>>();
            options.Setup(o => o.Get(It.IsAny<string>()))
                .Returns(new AuthenticationSchemeOptions());

            var handler = new GatewayAuthenticationHandler(
                options.Object, new LoggerFactory(), UrlEncoder.Default);

            var ctx = new DefaultHttpContext();
            var scheme = new AuthenticationScheme(
                GatewayAuthenticationHandler.SchemeName, null,
                typeof(GatewayAuthenticationHandler));

            await handler.InitializeAsync(scheme, ctx);
            return (handler, ctx);
        }

        [Fact]
        public async Task Authenticate_AllHeaders_BuildsCorrectPrincipal()
        {
            // Arrange
            var (handler, ctx) = await BuildHandlerAsync();
            var userId = Guid.NewGuid().ToString();
            ctx.Request.Headers["X-User-Id"] = userId;
            ctx.Request.Headers["X-User-Email"] = "staff@airport.dk";
            ctx.Request.Headers["X-User-Name"] = "jdoe";
            ctx.Request.Headers["X-User-Roles"] = "Staff";

            // Act
            var result = await handler.AuthenticateAsync();

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Principal!.FindFirstValue(ClaimTypes.NameIdentifier).Should().Be(userId);
            result.Principal!.FindFirstValue(ClaimTypes.Email).Should().Be("staff@airport.dk");
            result.Principal!.FindFirstValue(ClaimTypes.Name).Should().Be("jdoe");
            result.Principal!.IsInRole("Staff").Should().BeTrue();
        }

        [Fact]
        public async Task Authenticate_MultipleRoles_AllRoleClaimsPresent()
        {
            // Arrange
            var (handler, ctx) = await BuildHandlerAsync();
            ctx.Request.Headers["X-User-Id"] = Guid.NewGuid().ToString();
            ctx.Request.Headers["X-User-Roles"] = "Staff,Admin";

            // Act
            var result = await handler.AuthenticateAsync();

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Principal!.IsInRole("Staff").Should().BeTrue();
            result.Principal!.IsInRole("Admin").Should().BeTrue();
        }

        [Fact]
        public async Task Authenticate_MissingUserId_ReturnsNoResult()
        {
            // Arrange — only optional headers, X-User-Id absent
            var (handler, ctx) = await BuildHandlerAsync();
            ctx.Request.Headers["X-User-Email"] = "someone@airport.dk";

            // Act
            var result = await handler.AuthenticateAsync();

            // Assert — NoResult ≠ Fail; must not produce a 401 on its own
            result.Succeeded.Should().BeFalse();
            result.Failure.Should().BeNull();
        }

        [Fact]
        public async Task Authenticate_EmptyRolesHeader_NoRoleClaimsAdded()
        {
            // Arrange
            var (handler, ctx) = await BuildHandlerAsync();
            ctx.Request.Headers["X-User-Id"] = Guid.NewGuid().ToString();
            ctx.Request.Headers["X-User-Roles"] = "";

            // Act
            var result = await handler.AuthenticateAsync();

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Principal!.FindAll(ClaimTypes.Role).Should().BeEmpty();
        }

        [Fact]
        public async Task Authenticate_Ticket_UsesGatewaySchemeName()
        {
            // Arrange
            var (handler, ctx) = await BuildHandlerAsync();
            ctx.Request.Headers["X-User-Id"] = Guid.NewGuid().ToString();

            // Act
            var result = await handler.AuthenticateAsync();

            // Assert — scheme name must round-trip through the ticket
            result.Ticket!.AuthenticationScheme.Should().Be(GatewayAuthenticationHandler.SchemeName);
            result.Principal!.Identity!.AuthenticationType.Should().Be(GatewayAuthenticationHandler.SchemeName);
        }
    }

// 2. ClaimsPrincipalExtensions

    public class ClaimsPrincipalExtensionsTests
    {
        private static ClaimsPrincipal Build(params Claim[] claims)
            => new(new ClaimsIdentity(claims, "test"));

        // ── GetUserId 

        [Fact]
        public void GetUserId_ValidGuid_ReturnsParsedGuid()
        {
            // Arrange
            var expected = Guid.NewGuid();
            var principal = Build(new Claim(ClaimTypes.NameIdentifier, expected.ToString()));

            // Act & Assert
            principal.GetUserId().Should().Be(expected);
        }

        [Fact]
        public void GetUserId_MissingClaim_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            var principal = Build();

            // Act & Assert
            principal.Invoking(p => p.GetUserId())
                .Should().Throw<UnauthorizedAccessException>();
        }

        [Fact]
        public void GetUserId_NonGuidValue_ThrowsFormatException()
        {
            // Arrange
            var principal = Build(new Claim(ClaimTypes.NameIdentifier, "not-a-guid"));

            // Act & Assert
            principal.Invoking(p => p.GetUserId())
                .Should().Throw<FormatException>();
        }

        // ── GetEmail ──────────────────────────────────────────────────────────────

        [Fact]
        public void GetEmail_ClaimPresent_ReturnsEmail()
        {
            var principal = Build(new Claim(ClaimTypes.Email, "admin@airport.dk"));
            principal.GetEmail().Should().Be("admin@airport.dk");
        }

        [Fact]
        public void GetEmail_MissingClaim_ThrowsUnauthorizedAccessException()
        {
            Build().Invoking(p => p.GetEmail())
                .Should().Throw<UnauthorizedAccessException>();
        }

        // ── GetRole

        [Fact]
        public void GetRole_SingleRole_ReturnsIt()
        {
            var principal = Build(new Claim(ClaimTypes.Role, "Admin"));
            principal.GetRole().Should().Be("Admin");
        }

        [Fact]
        public void GetRole_MultipleRoles_ReturnsFirst()
        {
            // FindFirstValue returns whichever claim was added first
            var principal = Build(
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim(ClaimTypes.Role, "Staff"));

            principal.GetRole().Should().Be("Admin");
        }

        [Fact]
        public void GetRole_MissingClaim_ThrowsUnauthorizedAccessException()
        {
            Build().Invoking(p => p.GetRole())
                .Should().Throw<UnauthorizedAccessException>();
        }
    }


// 3. Role hierarchy boundary 

    public class RoleHierarchyTests
    {
        private static ClaimsPrincipal BuildPrincipal(params string[] roles)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
            };
            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
            return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        }

        [Fact]
        public async Task AdminAndStaff_AdminWinsRegardlessOfClaimOrder()
        {
            // Arrange — Staff listed before Admin in the claims
            var db = DbContextFactory.Create();
            var sut = new UserSyncService(db);
            var principal = BuildPrincipal("Staff", "Admin");

            // Act
            var user = await sut.SyncAsync(principal);

            // Assert — Admin check happens first in ResolveRole
            user.Role.Should().Be(UserRole.Admin);
        }

        [Fact]
        public async Task UnrecognisedRole_FallsBackToPassenger()
        {
            // Arrange
            var db = DbContextFactory.Create();
            var sut = new UserSyncService(db);
            var principal = BuildPrincipal("SuperUser");

            // Act
            var user = await sut.SyncAsync(principal);

            // Assert
            user.Role.Should().Be(UserRole.Passenger);
        }
    }
}

