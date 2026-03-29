using Microsoft.EntityFrameworkCore;
using BookingService.Data;
using  BookingService.Models;

namespace BookingService.Repositories;

public class PricingRepository : IPricingRepository
{
    private readonly BookingDbContext _context;
    
    public PricingRepository (BookingDbContext context)
    {
        _context = context;
    }

    public async Task<Pricing> GetPricingAsync()
    {
        return await _context.Pricings.FirstOrDefaultAsync();
    }
}