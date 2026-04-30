namespace BookingService.Messaging;

public class NotificationMessage
{
    public string FromName { get; set; } = "";
    public string ToEmail { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Body { get; set; } = "";
}