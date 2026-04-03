namespace BookingService.Models;

public class Passenger
{
    public Guid Id {get; set;}
    public Guid BookingId { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public DateTime DateOfBirth { get; set; }
    public string PassportNumber { get; set; }
    public string Nationality { get; set; }
    public string? SeatNumber { get; set; }
    public bool IsLeadPassenger { get; set; }
    public bool HasExtraBaggage { get; set; }
}