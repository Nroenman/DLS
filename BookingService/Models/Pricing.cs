namespace BookingService.Models;

public class Pricing
{
    public Guid Id {get; set;}
    public decimal ExtraBaggageFee {get; set;}
    public decimal ChildDiscount {get; set;}
}