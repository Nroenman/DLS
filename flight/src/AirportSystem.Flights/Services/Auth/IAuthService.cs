using AirportSystem.Flights.Models;

namespace AirportSystem.Flights.Services.Auth;

public interface IAuthService
{
    Task<(User User, string Token)> RegisterAsync(
        string username, string email, string password, UserRole role = UserRole.Passenger);

    Task<(User User, string Token)> LoginAsync(string email, string password);
}
