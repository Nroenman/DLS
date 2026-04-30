using AirportSystem.Flights.Models;

namespace AirportSystem.Flights.Services.Auth;

public interface ITokenService
{
    string GenerateToken(User user);
}
