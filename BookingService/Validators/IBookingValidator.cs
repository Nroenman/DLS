using BookingService.DTO;
using BookingService.Models;

namespace BookingService.Validators;

public interface IBookingValidator
{
    void ValidateCreateBooking(CreateBookingRequest request);
    void ValidateGetBookingsByUserId(string userId);
    void ValidateUpdateBookingStatus(BookingStatus status);
    void ValidateCancelBooking(Booking booking, string userId);
}