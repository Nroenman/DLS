using BookingService.Models;

namespace BookingService.DTO;

public class BookingResponse
{
    public Guid BookingId { get; set; }
    public BookingStatus Status { get; set; }
    public string FlightId { get; set; }
    public string? ReturnFlightId { get; set; }
    public SeatClass? SeatClass { get; set; }
    public decimal TotalPrice { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<PassengerResponse> Passengers { get; set; }
}