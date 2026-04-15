namespace AirportSystem.Flights.Models;

public class Gate
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string GateNumber { get; set; } = string.Empty;
    public string Terminal { get; set; } = string.Empty;
    public bool IsAvailable { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<Flight> Flights { get; set; } = new List<Flight>();
}
