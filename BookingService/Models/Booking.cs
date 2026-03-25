namespace BookingService.Models;

public class Booking
{
    public Guid Id { get; set; }
    public string UserId { get; set; }
    public string FlightId { get; set; }
    public string? ReturnFlightId { get; set; }
    public  bool IsOneWay { get; set; }
    public string SeatClass  { get; set; }
    public decimal TotalPrice { get; set; }
    public int NumberOfPassengers { get; set; }
    public List<Passenger> Passengers { get; set; }
    public string Status { get; set; }
    public string ContactEmail { get; set; }
    public string ContactPhone { get; set; }
    public DateTime CreatedAt { get; set; }
}