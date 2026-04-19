using BookingService.Models;
using BookingService.DTO;

namespace BookingService.Validators;

public class BookingValidator : IBookingValidator
{
    public void ValidatePassengerCount(List<CreatePassengerRequest> passengers)
    {
        if (passengers == null)
            throw new ArgumentException("Number of passengers must be greater than 0");
        
        if (passengers.Count < 1 || passengers.Count > 9)
            throw new ArgumentException("Number of passengers must be between 1 and 9");
    }

    public void ValidateLeadPassenger(List<CreatePassengerRequest> passengers)
    {
        if (passengers.Count(p => p.IsLeadPassenger) != 1)
            throw new ArgumentException("Exactly one lead passenger must be designated");
    }

    public void ValidatePassengerDetails(List<CreatePassengerRequest> passengers)
    {
        if (passengers.Any(p => string.IsNullOrEmpty(p.FirstName)))
            throw new ArgumentException("First name is required");
        
        if (passengers.Any(p => string.IsNullOrEmpty(p.LastName)))
            throw new ArgumentException("Last name is required");
        
        if (passengers.Any(p => p.DateOfBirth == default))
            throw new ArgumentException("Date of birth is required");
        
        if (passengers.Any(p => p.DateOfBirth > DateTime.Today))
            throw new ArgumentException("Date of birth must be in the past");
        
        if (passengers.Any(p => string.IsNullOrEmpty(p.PassportNumber)))
            throw new ArgumentException("Passport number is required");
        
        if (passengers.Any(p => string.IsNullOrEmpty(p.Nationality)))
            throw new ArgumentException("Nationality is required");
    }

    public void ValidateFlightInfo(CreateBookingRequest request)
    {
        if (string.IsNullOrEmpty(request.FlightId))
            throw new ArgumentException("Flight ID is required");
        
        if (!request.IsOneWay && string.IsNullOrEmpty(request.ReturnFlightId))
            throw new ArgumentException("Return flight ID is required for return tickets");
        
        if (request.IsOneWay && !string.IsNullOrEmpty(request.ReturnFlightId))
            throw new ArgumentException("Return flight ID cannot be provided for a one-way ticket");
    }

    public void ValidateBookingDetails(CreateBookingRequest request)
    {
        if (request.SeatClass == null)
            throw new ArgumentException("Seat class is required");
        
        if (request.TicketPrice <= 0)
            throw new ArgumentException("Ticket price must be greater than 0");
        
        if (string.IsNullOrEmpty(request.ContactEmail))
            throw new ArgumentException("Contact email is required");
        
        if (!System.Text.RegularExpressions.Regex.IsMatch(request.ContactEmail, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            throw new ArgumentException("Contact email format is invalid");
        
        if (string.IsNullOrEmpty(request.ContactPhone))
            throw new ArgumentException("Contact phone number is required");
    }

    public void ValidateGetBookingsByUserId(string userId)
    {
        if (string.IsNullOrEmpty(userId))
            throw new ArgumentException("User ID is required");
    }

    public void ValidateUpdateBookingStatus(BookingStatus status)
    {
        if (!Enum.IsDefined(typeof(BookingStatus), status))
            throw new ArgumentException("Invalid booking status");
    }

    public void ValidateCancelBooking(Booking booking, string userId)
    {
        if (booking == null)
            throw new ArgumentException("Booking not found");
    
        if (booking.UserId != userId)
            throw new ArgumentException("Not authorized");
    
        if (booking.Status == BookingStatus.Cancelled)
            throw new ArgumentException("Booking is already cancelled");
    }
}