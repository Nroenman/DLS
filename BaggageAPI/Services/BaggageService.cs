using BaggageAPI.Data;
using BaggageAPI.Dtos;
using BaggageAPI.Interfaces;
using BaggageAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace BaggageAPI.Services;

public class BaggageService(AppDbContext context, RabbitMqService rabbitMq) : IBaggageService
{
    private readonly AppDbContext _context = context;
    private readonly RabbitMqService _rabbitMq = rabbitMq; 

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

        _rabbitMq.Publish("baggagequeue", new
        {
            Event = "BaggageCheckedIn",
            Data = baggage
        });

        return baggage;
    } 

    public async Task<Baggage?> UpdateStatusAsync(Guid id, UpdateBaggageStatusDto dto)
    {
        var baggage = await _context.Baggages.FindAsync(id);

        if (baggage is null) return null; // ✅ Fix 4: null guard

        baggage.Status = dto.Status;
        baggage.CurrentLocation = dto.Location;

        await _context.SaveChangesAsync();

        
        _rabbitMq.Publish("baggagequeue", new
        {
            Event = "BaggageStatusUpdated",
            Data = baggage
        });

        return baggage;
    }

    public async Task<List<Baggage>> GetByPassenger(Guid passengerId)
    {
        return await _context.Baggages
            .Where(x => x.PassengerId == passengerId)
            .ToListAsync();
    }
}