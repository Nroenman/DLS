using BookingService.Models;
using BookingService.DTO;

namespace BookingService.Validators;

public class BookingValidator
{
    public static void ValidateCreateBooking(CreateBookingRequest request)
    {
        //Passenger
        if (request.Passengers == null)
            throw new ArgumentException("Passengers count must be greater than 0");
        
        if (request.Passengers.Count < 1 || request.Passengers.Count > 9)
            throw new ArgumentException("Number of passengers must be between 1 and 9");
        
        if (request.Passengers.Count(p => p.IsLeadPassenger) != 1)
            throw new ArgumentException("Exactly one lead passenger must be designated");
        
        if (request.Passengers.Any(p => string.IsNullOrEmpty(p.FirstName) || string.IsNullOrEmpty(p.LastName)))
            throw new ArgumentException("Passenger first name and last name are required");
        
        if (request.Passengers.Any(p => p.DateOfBirth == default))
            throw new ArgumentException("Passenger date of birth is required");
        
        if (request.Passengers.Any(p => string.IsNullOrEmpty(p.PassportNumber)))
            throw new ArgumentException("Passenger passport number is required");
        
        if (request.Passengers.Any(p => string.IsNullOrEmpty(p.Nationality)))
            throw new ArgumentException("Passenger nationality is required");
            
        //Flight
        if (string.IsNullOrEmpty(request.FlightId))
            throw new ArgumentException("Flight ID is required");
        
        if (!request.IsOneWay && string.IsNullOrEmpty(request.ReturnFlightId))
            throw new ArgumentException("Return flight ID is required for return tickets");
        
        if (request.IsOneWay && !string.IsNullOrEmpty(request.ReturnFlightId))
            throw new ArgumentException("Return flight ID cannot be provided for a one-way ticket");
        
        //Booking
        if (request.SeatClass == null)
            throw new ArgumentException("SeatClass is required");
        
        if (request.TicketPrice <= 0)
            throw new ArgumentException("Ticket price must be greater than 0");
        
        if (string.IsNullOrEmpty(request.ContactEmail))
            throw new ArgumentException("Contact email is required");
        
        if (string.IsNullOrEmpty(request.ContactPhone))
            throw new ArgumentException("Contact phone number is required");
    }

    public static void ValidateGetBookingsByUserId(string userId)
    {
        if (string.IsNullOrEmpty(userId))
            throw new ArgumentException("User ID is required");
    }

    public static void ValidateUpdateBookingStatus(BookingStatus status)
    {
        if (!Enum.IsDefined(typeof(BookingStatus), status))
            throw new ArgumentException("Invalid booking status");
    }
}