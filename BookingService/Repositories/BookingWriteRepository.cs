using BookingService.Data;
using BookingService.Models;

namespace BookingService.Repositories;

public class BookingWriteRepository : IBookingWriteRepository
{
    private readonly BookingDbContext _context;
    
    public BookingWriteRepository(BookingDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Booking booking)
    {
        await _context.Bookings.AddAsync(booking);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateStatusAsync(Guid id, BookingStatus status)
    {
        var booking = await _context.Bookings.FindAsync(id);
        if (booking != null)
        {
            booking.Status = status;
            await _context.SaveChangesAsync();
        }
    }
}