using BookingService.Models;

namespace BookingService.DTO;

public class CreateBookingRequest
{
    public string FlightId { get; set; }
    public string? ReturnFlightId { get; set; }
    public bool IsOneWay { get; set; }
    public SeatClass? SeatClass {get; set;}
    public string ContactEmail {get; set;}
    public string ContactPhone {get; set;}
    public decimal TicketPrice {get; set;}
    public List<CreatePassengerRequest> Passengers {get; set;}
}