using AirportSystem.Flights.Models;
using AirportSystem.Flights.Services.Auth;
using AirportSystem.Tests.Helpers;
using FluentAssertions;
using Moq;

namespace AirportSystem.Tests.Services;

public class AuthServiceTests
{
    private static (AuthService service, Mock<ITokenService> tokenMock) CreateService(
        string? dbName = null)
    {
        var db          = DbContextFactory.Create(dbName);
        var tokenMock   = new Mock<ITokenService>();
        tokenMock.Setup(t => t.GenerateToken(It.IsAny<User>())).Returns("mock-jwt-token");

        var service = new AuthService(db, tokenMock.Object);
        return (service, tokenMock);
    }

    // ── Register ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Register_ValidInput_ReturnsUserAndToken()
    {
        var (service, _) = CreateService();

        var (user, token) = await service.RegisterAsync("alice", "alice@test.com", "Pass123!");

        user.Should().NotBeNull();
        user.Username.Should().Be("alice");
        user.Email.Should().Be("alice@test.com");
        user.Role.Should().Be(UserRole.Passenger);
        user.PasswordHash.Should().NotBe("Pass123!"); // must be hashed
        token.Should().Be("mock-jwt-token");
    }

    [Fact]
    public async Task Register_DuplicateEmail_ThrowsInvalidOperationException()
    {
        var (service, _) = CreateService();
        await service.RegisterAsync("alice", "alice@test.com", "Pass123!");

        var act = async () =>
            await service.RegisterAsync("alice2", "alice@test.com", "Pass456!");

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*already registered*");
    }

    [Fact]
    public async Task Register_DuplicateUsername_ThrowsInvalidOperationException()
    {
        var (service, _) = CreateService();
        await service.RegisterAsync("alice", "alice@test.com", "Pass123!");

        var act = async () =>
            await service.RegisterAsync("alice", "other@test.com", "Pass456!");

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*already taken*");
    }

    [Fact]
    public async Task Register_WithAdminRole_PersistsRole()
    {
        var (service, _) = CreateService();

        var (user, _) = await service.RegisterAsync(
            "adminuser", "admin@test.com", "Pass123!", UserRole.Admin);

        user.Role.Should().Be(UserRole.Admin);
    }

    // ── Login ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_ValidCredentials_ReturnsUserAndToken()
    {
        var (service, _) = CreateService();
        await service.RegisterAsync("bob", "bob@test.com", "Secret99!");

        var (user, token) = await service.LoginAsync("bob@test.com", "Secret99!");

        user.Should().NotBeNull();
        user.Email.Should().Be("bob@test.com");
        token.Should().Be("mock-jwt-token");
    }

    [Fact]
    public async Task Login_WrongPassword_ThrowsUnauthorizedAccessException()
    {
        var (service, _) = CreateService();
        await service.RegisterAsync("bob", "bob@test.com", "Secret99!");

        var act = async () =>
            await service.LoginAsync("bob@test.com", "WrongPass!");

        await act.Should()
            .ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*Invalid email or password*");
    }

    [Fact]
    public async Task Login_UnknownEmail_ThrowsUnauthorizedAccessException()
    {
        var (service, _) = CreateService();

        var act = async () =>
            await service.LoginAsync("nobody@test.com", "whatever");

        await act.Should()
            .ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*Invalid email or password*");
    }
}
