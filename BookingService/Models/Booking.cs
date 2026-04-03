namespace BookingService.Models;

public class Booking
{
    public Guid Id { get; set; }
    public string UserId { get; set; }
    public string FlightId { get; set; }
    public string? ReturnFlightId { get; set; }
    public  bool IsOneWay { get; set; }
    public SeatClass? SeatClass  { get; set; }
    public decimal TotalPrice { get; set; }
    public List<Passenger> Passengers { get; set; }
    public BookingStatus Status { get; set; }
    public string ContactEmail { get; set; }
    public string ContactPhone { get; set; }
    public DateTime CreatedAt { get; set; }
}