using AirportSystem.Flights.Data;
using AirportSystem.Flights.Models;
using Microsoft.EntityFrameworkCore;

namespace AirportSystem.Flights.Services.Auth;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly ITokenService _tokenService;

    public AuthService(AppDbContext db, ITokenService tokenService)
    {
        _db = db;
        _tokenService = tokenService;
    }

    public async Task<(User User, string Token)> RegisterAsync(
        string username, string email, string password, UserRole role = UserRole.Passenger)
    {
        if (await _db.Users.AnyAsync(u => u.Email == email))
            throw new InvalidOperationException($"Email '{email}' is already registered.");

        if (await _db.Users.AnyAsync(u => u.Username == username))
            throw new InvalidOperationException($"Username '{username}' is already taken.");

        var user = new User
        {
            Username = username,
            Email    = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role     = role
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var token = _tokenService.GenerateToken(user);
        return (user, token);
    }

    public async Task<(User User, string Token)> LoginAsync(string email, string password)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email)
            ?? throw new UnauthorizedAccessException("Invalid email or password.");

        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password.");

        var token = _tokenService.GenerateToken(user);
        return (user, token);
    }
}
