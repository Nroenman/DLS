namespace BaggageAPI.Models;

public class Baggage
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public Guid PassengerId { get; set; }

    public double Weight { get; set; }

    public BaggageStatus Status { get; set; }
    public string CurrentLocation { get; set; }

    public DateTime CreatedAt { get; set; }
}