using BookingService.Models;

namespace BookingService.Repositories;

public interface IBookingReadRepository
{
    Task<Booking?> GetByIdAsync(Guid id);
    Task<List<Booking>> GetByUserIdAsync(string userId);
}