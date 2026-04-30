namespace BookingService.Messaging;

public interface IBookingEventPublisher
{
    Task PublishPaymentMessage(PaymentMessage message);
    Task PublishNotificationMessage(NotificationMessage message);
}