namespace BookingService.DTO;

public class PassengerResponse
{
    public Guid Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string? SeatNumber { get; set; }
    public bool IsLeadPassenger { get; set; }
    public bool HasExtraBaggage { get; set; }
}