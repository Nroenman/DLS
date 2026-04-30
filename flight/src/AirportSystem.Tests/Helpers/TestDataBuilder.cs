using AirportSystem.Flights.Data;
using AirportSystem.Flights.Models;

namespace AirportSystem.Tests.Helpers;

/// <summary>
/// Fluent builder for seeding test data into an AppDbContext.
/// Passwords are no longer stored locally — Keycloak owns them.
/// </summary>
public class TestDataBuilder
{
    private readonly AppDbContext _db;

    public TestDataBuilder(AppDbContext db) => _db = db;

    public User SeedUser(
        string username  = "testuser",
        string email     = "test@example.com",
        UserRole role    = UserRole.Passenger,
        Guid? id         = null)
    {
        var user = new User
        {
            Id        = id ?? Guid.NewGuid(),   // simulates Keycloak sub
            Username  = username,
            Email     = email,
            Role      = role,
            CreatedAt  = DateTime.UtcNow,
            LastSeenAt = DateTime.UtcNow
        };
        _db.Users.Add(user);
        _db.SaveChanges();
        return user;
    }

    public Gate SeedGate(string gateNumber = "A1", string terminal = "A")
    {
        var gate = new Gate { GateNumber = gateNumber, Terminal = terminal };
        _db.Gates.Add(gate);
        _db.SaveChanges();
        return gate;
    }

    public Flight SeedFlight(
        string flightNumber           = "SK101",
        FlightDirection direction      = FlightDirection.Departure,
        FlightStatus status            = FlightStatus.Scheduled,
        Guid? gateId                  = null)
    {
        var now = DateTime.UtcNow;
        var flight = new Flight
        {
            FlightNumber       = flightNumber,
            Airline            = "SAS",
            Origin             = "CPH",
            Destination        = "LHR",
            ScheduledDeparture = now.AddHours(2),
            ScheduledArrival   = now.AddHours(4),
            Direction          = direction,
            Status             = status,
            GateId             = gateId
        };
        _db.Flights.Add(flight);
        _db.SaveChanges();
        return flight;
    }
}
