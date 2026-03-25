using Microsoft.EntityFrameworkCore;
using BookingService.Data;
using BookingService.Models;


namespace BookingService.Repositories;

public class BookingReadRepository : IBookingReadRepository
{
    private readonly BookingDbContext _context;
    
    public BookingReadRepository(BookingDbContext context)
    {
        _context = context;
    }
    
    public async Task<Booking?> GetByIdAsync(Guid id)
    {
        return await _context.Bookings
            .Include(b => b.Passengers)
            .FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task<List<Booking>> GetByUserIdAsync(string userId)
    {
        return await _context.Bookings
            .Include(b => b.Passengers)
            .Where(b => b.UserId == userId)
            .ToListAsync();
    }
}