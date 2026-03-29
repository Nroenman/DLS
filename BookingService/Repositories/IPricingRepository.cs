using BookingService.Models;

namespace BookingService.Repositories;

public interface IPricingRepository
{
    Task<Pricing> GetPricingAsync();
}