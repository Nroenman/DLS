namespace BookingService.Messaging;

public class PaymentMessage
{
    public Guid BookingId { get; set; }
    public string UserId { get; set; } = "";
    public decimal TotalPrice { get; set; }
    public string ContactEmail { get; set; } = "";
    public string ContactPhone { get; set; } = "";
}