using BaggageAPI.Data;
using BaggageAPI.Dtos;
using BaggageAPI.Interfaces;
using BaggageAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace BaggageAPI.Services;

public class BaggageService(AppDbContext context) : IBaggageService
{
    private readonly AppDbContext _context = context;

    public async Task<Baggage> CreateAsync(CreateBaggageDto dto)
    {
        var baggage = new Baggage
        {
            Id = Guid.NewGuid(),
            BookingId = dto.BookingId,
            PassengerId = dto.PassengerId,
            Weight = dto.Weight,
            Status = BaggageStatus.CheckedIn,
            CurrentLocation = "Check-in",
            CreatedAt = DateTime.UtcNow
        };

        _context.Baggages.Add(baggage);
        await _context.SaveChangesAsync();

        //  Send event via RabbitMQ
        // "BaggageCheckedIn"

        return baggage;
    }

    public async Task<Baggage> UpdateStatusAsync(Guid id, UpdateBaggageStatusDto dto)
    {
        var baggage = await _context.Baggages.FindAsync(id);

        baggage.Status = dto.Status;
        baggage.CurrentLocation = dto.Location;

        await _context.SaveChangesAsync();

        // 🔔 Publish event: BaggageStatusUpdated

        return baggage;
    }

    public async Task<List<Baggage>> GetByPassenger(Guid passengerId)
    {
        return await _context.Baggages
            .Where(x => x.PassengerId == passengerId)
            .ToListAsync();
    }
}