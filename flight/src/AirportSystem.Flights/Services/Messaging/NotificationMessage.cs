namespace AirportSystem.Flights.Services.Messaging;

/// <summary>
/// Wire contract for the NotificationService's "Notification" queue. The
/// property names must match NotificationService's NotificationMessage so it
/// can deserialize what we publish.
/// </summary>
public record NotificationMessage(
    string FromName,
    string ToEmail,
    string Subject,
    string Body);
