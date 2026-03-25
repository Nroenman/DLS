using BookingService.DTO;
using BookingService.Models;
using BookingService.Repositories;

namespace BookingService.Services;

public class BookingService : IBookingService
{
    private readonly IBookingReadRepository _bookingReadRepository;
    private readonly IBookingWriteRepository _bookingWriteRepository;
    
    public BookingService(IBookingReadRepository bookingReadRepository, IBookingWriteRepository bookingWriteRepository)
        {
        _bookingReadRepository = bookingReadRepository;
        _bookingWriteRepository = bookingWriteRepository;
        }

    public Task<BookingResponse> CreateBookingAsync(CreateBookingRequest request, string userId)
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

    }

    public Task<BookingResponse> GetBookingByIdAsync(Guid id)
    {
        
    }

    public Task<List<BookingResponse>> GetBookingsByUserIdAsync(string userId)
    {
        
    }

    public Task UpdateBookingStatusAsync(Guid id, string status)
    {
        
    }
}