using BookingService.DTO;
using BookingService.Models;
using BookingService.Repositories;

namespace BookingService.Services;

public class BookingService : IBookingService
{
    private readonly IBookingReadRepository _bookingReadRepository;
    private readonly IBookingWriteRepository _bookingWriteRepository;
    private readonly IPricingRepository _pricingRepository;
    
    public BookingService(
        IBookingReadRepository bookingReadRepository, 
        IBookingWriteRepository bookingWriteRepository,  
        IPricingRepository pricingRepository)
        {
        _bookingReadRepository = bookingReadRepository;
        _bookingWriteRepository = bookingWriteRepository;
        _pricingRepository = pricingRepository;
        }

    public async Task<BookingResponse> CreateBookingAsync(CreateBookingRequest request, string userId)
    {
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FlightId = request.FlightId,
            ReturnFlightId = request.ReturnFlightId,
            IsOneWay = request.IsOneWay,
            SeatClass = request.SeatClass,
            Status = "Pending",
            ContactEmail = request.ContactEmail,
            ContactPhone = request.ContactPhone,
            CreatedAt = DateTime.UtcNow,
        };

        var passengers = request.Passengers.Select(p => new Passenger
        {
            Id = Guid.NewGuid(),
            BookingId = booking.Id,
            FirstName = p.FirstName,
            LastName = p.LastName,
            DateOfBirth = p.DateOfBirth,
            PassportNumber = p.PassportNumber,
            Nationality = p.Nationality,
            SeatNumber = null, // Seat assignment will be handled later
            IsLeadPassenger = p.IsLeadPassenger,
            HasExtraBaggage = p.HasExtraBaggage
        }).ToList();
        
        booking.Passengers = passengers;
        var pricing = await _pricingRepository.GetPricingAsync();

        booking.TotalPrice = passengers.Sum(p =>
        {
            var price = request.TicketPrice;
            var age = DateTime.Today.Year - p.DateOfBirth.Year;
            
            if (p.DateOfBirth.Date > DateTime.Today.AddYears(-age))
                age--;

            if (age < 12)
            {
                var discount = request.TicketPrice * pricing.ChildDiscount;
                price = request.TicketPrice - discount;
            }
            
            if (p.HasExtraBaggage)
                price += pricing.ExtraBaggageFee;

            return price;
        });
        await _bookingWriteRepository.AddAsync(booking);

        BookingResponse response = new BookingResponse()
        {
            BookingId = booking.Id,
            Status = booking.Status,
            FlightId = booking.FlightId,
            ReturnFlightId = booking.ReturnFlightId,
            SeatClass = booking.SeatClass,
            TotalPrice = booking.TotalPrice,
            CreatedAt = booking.CreatedAt,
            Passengers = passengers.Select(p => new PassengerResponse()
            {
                Id = p.Id,
                FirstName = p.FirstName,
                LastName = p.LastName,
                SeatNumber = p.SeatNumber,
                IsLeadPassenger = p.IsLeadPassenger,
                HasExtraBaggage = p.HasExtraBaggage
            }).ToList()
        };
        
        return response;
    }

    public async Task<BookingResponse?> GetBookingAsync(Guid id)
    {
        var booking = await _bookingReadRepository.GetByIdAsync(id);
        if (booking == null)
            return null;

        BookingResponse response = new BookingResponse()
        {
            BookingId = booking.Id,
            Status = booking.Status,
            FlightId = booking.FlightId,
            ReturnFlightId = booking.ReturnFlightId,
            SeatClass = booking.SeatClass,
            TotalPrice = booking.TotalPrice,
            CreatedAt = booking.CreatedAt,
            Passengers = booking.Passengers.Select(p => new PassengerResponse()
            {
                Id = p.Id,
                FirstName = p.FirstName,
                LastName = p.LastName,
                SeatNumber = p.SeatNumber,
                IsLeadPassenger = p.IsLeadPassenger,
                HasExtraBaggage = p.HasExtraBaggage
            }).ToList()
        };
        return response;
    }

    public async Task<List<BookingResponse>> GetBookingsByUserIdAsync(string userId)
    {
        var booking = await  _bookingReadRepository.GetByUserIdAsync(userId);

        var responses = booking.Select(b => new BookingResponse()
        {
            BookingId = b.Id,
            Status = b.Status,
            FlightId = b.FlightId,
            ReturnFlightId = b.ReturnFlightId,
            SeatClass = b.SeatClass,
            TotalPrice = b.TotalPrice,
            CreatedAt = b.CreatedAt,
            Passengers = b.Passengers.Select(p => new PassengerResponse()
            {
                Id = p.Id,
                FirstName = p.FirstName,
                LastName = p.LastName,
                SeatNumber = p.SeatNumber,
                IsLeadPassenger = p.IsLeadPassenger,
                HasExtraBaggage = p.HasExtraBaggage
            }).ToList()
        }).ToList();
        return responses;
    }

    public async Task UpdateBookingStatusAsync(Guid id, string status)
    {
        await _bookingWriteRepository.UpdateStatusAsync(id, status);
    }
}