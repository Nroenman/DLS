namespace AirportSystem.Flights.Models;

public class Booking
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid FlightId { get; set; }
    public Flight Flight { get; set; } = null!;
    public string? SeatNumber { get; set; }
    public DateTime BookedAt { get; set; } = DateTime.UtcNow;
}
