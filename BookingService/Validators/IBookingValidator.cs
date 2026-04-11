using BookingService.DTO;
using BookingService.Models;

namespace BookingService.Validators;

public interface IBookingValidator
{
    void ValidatePassengerCount(List<CreatePassengerRequest>? passengers);
    void ValidateLeadPassenger(List<CreatePassengerRequest> passengers);
    void ValidatePassengerDetails(List<CreatePassengerRequest> passengers);
    void ValidateFlightInfo(CreateBookingRequest request);
    void ValidateBookingDetails(CreateBookingRequest request);
    void ValidateGetBookingsByUserId(string userId);
    void ValidateUpdateBookingStatus(BookingStatus status);
}