using BookingService.DTO;
using BookingService.Models;

namespace BookingService.Services;

public interface IBookingService
{
    Task<BookingResponse> CreateBookingAsync(CreateBookingRequest request, string userId);
    Task<BookingResponse> GetBookingByIdAsync(Guid id);
    Task<List<BookingResponse>> GetBookingsByUserIdAsync(string userId);
    Task UpdateBookingStatusAsync(Guid id, BookingStatus status);
}