namespace BaggageAPI.Dtos;

public class CreateBaggageDto
{
    public Guid BookingId { get; set; }
    public Guid PassengerId { get; set; }
    public double Weight { get; set; }
}