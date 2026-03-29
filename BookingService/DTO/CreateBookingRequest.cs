namespace BookingService.DTO;

public class CreateBookingRequest
{
    public string FlightId { get; set; }
    public string? ReturnFlightId { get; set; }
    public bool IsOneWay { get; set; }
    public string SeatClass {get; set;}
    public string ContactEmail {get; set;}
    public string ContactPhone {get; set;}
    public decimal TicketPrice {get; set;}
    public List<CreatePassengerRequest> Passengers {get; set;}
}