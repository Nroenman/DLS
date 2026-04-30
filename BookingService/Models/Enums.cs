namespace BookingService.Models;

public enum BookingStatus
{
    Pending,
    AwaitingPayment,
    Confirmed,
    Cancelled
}

public enum SeatClass
{
    Economy,
    Business,
    First
}