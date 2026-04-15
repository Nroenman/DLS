namespace AirportSystem.Flights.Models;

public class FlightFollow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid FlightId { get; set; }
    public Flight Flight { get; set; } = null!;
    public DateTime FollowedAt { get; set; } = DateTime.UtcNow;
}
