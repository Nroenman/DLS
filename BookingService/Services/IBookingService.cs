using BookingService.DTO;

namespace BookingService.Services;

public interface IBookingService
{
    Task<BookingResponse> CreateBookingAsync(CreateBookingRequest request, string userId);
    Task<BookingResponse> GetBookingAsync(Guid id);
    Task<List<BookingResponse>> GetBookingsByUserIdAsync(string userId);
    Task UpdateBookingStatusAsync(Guid id, string status);
}