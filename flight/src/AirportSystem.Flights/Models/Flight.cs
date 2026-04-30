namespace AirportSystem.Flights.Models;

public class Flight
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FlightNumber { get; set; } = string.Empty;
    public string Airline { get; set; } = string.Empty;
    public string Origin { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public DateTime ScheduledDeparture { get; set; }
    public DateTime ScheduledArrival { get; set; }
    public DateTime? ActualDeparture { get; set; }
    public DateTime? ActualArrival { get; set; }
    public FlightStatus Status { get; set; } = FlightStatus.Scheduled;
    public FlightDirection Direction { get; set; }
    public string? DelayReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Gate relationship
    public Guid? GateId { get; set; }
    public Gate? Gate { get; set; }

    // Navigation
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<FlightFollow> Followers { get; set; } = new List<FlightFollow>();
}
