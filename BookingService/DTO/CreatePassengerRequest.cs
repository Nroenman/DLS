namespace BookingService.DTO;

public class CreatePassengerRequest
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public DateTime DateOfBirth { get; set; }
    public string PassportNumber { get; set; }
    public string Nationality { get; set; }
    public bool HasExtraBaggage { get; set; }
}