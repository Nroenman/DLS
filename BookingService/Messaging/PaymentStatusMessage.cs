namespace BookingService.Messaging;

public class PaymentStatusMessage
{
    public Guid BookingId { get; set; }
    public bool PaymentSucceeded { get; set; }
}