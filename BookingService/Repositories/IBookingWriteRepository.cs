using BookingService.Models;

namespace BookingService.Repositories;

public interface IBookingWriteRepository
{
    Task AddAsync(Booking booking);
    Task UpdateStatusAsync(Guid id, string status);
}