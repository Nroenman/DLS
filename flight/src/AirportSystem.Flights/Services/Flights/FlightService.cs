using AirportSystem.Flights.Data;
using AirportSystem.Flights.Models;
using AirportSystem.Flights.Services.Messaging;
using Microsoft.EntityFrameworkCore;

namespace AirportSystem.Flights.Services.Flights;

public class FlightService : IFlightService
{
    private readonly AppDbContext _db;
    private readonly IFlightEventPublisher _eventPublisher;

    public FlightService(AppDbContext db, IFlightEventPublisher eventPublisher)
    {
        _db = db;
        _eventPublisher = eventPublisher;
    }

    // ── Create ────────────────────────────────────────────────────────────────

    public async Task<Flight> CreateFlightAsync(
        string flightNumber, string airline,
        string origin, string destination,
        DateTime scheduledDeparture, DateTime scheduledArrival,
        FlightDirection direction, Guid? gateId = null)
    {
        if (scheduledArrival <= scheduledDeparture)
            throw new ArgumentException("Arrival must be after departure.");

        if (gateId.HasValue && !await _db.Gates.AnyAsync(g => g.Id == gateId))
            throw new KeyNotFoundException($"Gate '{gateId}' not found.");

        var flight = new Flight
        {
            FlightNumber       = flightNumber,
            Airline            = airline,
            Origin             = origin,
            Destination        = destination,
            ScheduledDeparture = scheduledDeparture.ToUniversalTime(),
            ScheduledArrival   = scheduledArrival.ToUniversalTime(),
            Direction          = direction,
            GateId             = gateId
        };

        _db.Flights.Add(flight);
        await _db.SaveChangesAsync();
        return await GetFlightByIdAsync(flight.Id);
    }

    // ── Update ────────────────────────────────────────────────────────────────

    public async Task<Flight> UpdateFlightAsync(
        Guid id,
        FlightStatus? status = null,
        DateTime? actualDeparture = null,
        DateTime? actualArrival = null,
        string? delayReason = null,
        Guid? gateId = null)
    {
        var flight = await _db.Flights.FindAsync(id)
            ?? throw new KeyNotFoundException($"Flight '{id}' not found.");

        if (status.HasValue)
            flight.Status = status.Value;

        if (actualDeparture.HasValue)
            flight.ActualDeparture = actualDeparture.Value.ToUniversalTime();

        if (actualArrival.HasValue)
            flight.ActualArrival = actualArrival.Value.ToUniversalTime();

        if (delayReason is not null)
            flight.DelayReason = delayReason;

        if (gateId.HasValue)
        {
            if (!await _db.Gates.AnyAsync(g => g.Id == gateId))
                throw new KeyNotFoundException($"Gate '{gateId}' not found.");
            flight.GateId = gateId;
        }

        flight.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var updated = await GetFlightByIdAsync(flight.Id);
        _eventPublisher.PublishFlightUpdated(updated);
        return updated;
    }

    // ── Queries ───────────────────────────────────────────────────────────────

    public async Task<Flight> GetFlightByIdAsync(Guid id)
    {
        return await _db.Flights
            .Include(f => f.Gate)
            .Include(f => f.Bookings).ThenInclude(b => b.User)
            .Include(f => f.Followers).ThenInclude(ff => ff.User)
            .FirstOrDefaultAsync(f => f.Id == id)
            ?? throw new KeyNotFoundException($"Flight '{id}' not found.");
    }

    public async Task<IReadOnlyList<Flight>> GetAllFlightsAsync(
        FlightDirection? direction = null,
        FlightStatus? status = null)
    {
        var query = _db.Flights
            .Include(f => f.Gate)
            .AsQueryable();

        if (direction.HasValue)
            query = query.Where(f => f.Direction == direction.Value);

        if (status.HasValue)
            query = query.Where(f => f.Status == status.Value);

        return await query
            .OrderBy(f => f.ScheduledDeparture)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Flight>> GetFollowedFlightsByUserAsync(Guid userId)
    {
        return await _db.FlightFollows
            .Where(ff => ff.UserId == userId)
            .Include(ff => ff.Flight).ThenInclude(f => f.Gate)
            .Select(ff => ff.Flight)
            .OrderBy(f => f.ScheduledDeparture)
            .ToListAsync();
    }

    // ── Follows ───────────────────────────────────────────────────────────────

    public async Task<FlightFollow> FollowFlightAsync(Guid userId, Guid flightId)
    {
        if (!await _db.Users.AnyAsync(u => u.Id == userId))
            throw new KeyNotFoundException($"User '{userId}' not found.");

        if (!await _db.Flights.AnyAsync(f => f.Id == flightId))
            throw new KeyNotFoundException($"Flight '{flightId}' not found.");

        if (await _db.FlightFollows.AnyAsync(ff => ff.UserId == userId && ff.FlightId == flightId))
            throw new InvalidOperationException("You are already following this flight.");

        var follow = new FlightFollow { UserId = userId, FlightId = flightId };
        _db.FlightFollows.Add(follow);
        await _db.SaveChangesAsync();

        return await _db.FlightFollows
            .Include(ff => ff.Flight).ThenInclude(f => f.Gate)
            .Include(ff => ff.User)
            .FirstAsync(ff => ff.Id == follow.Id);
    }

    public async Task<bool> UnfollowFlightAsync(Guid userId, Guid flightId)
    {
        var follow = await _db.FlightFollows
            .FirstOrDefaultAsync(ff => ff.UserId == userId && ff.FlightId == flightId);

        if (follow is null) return false;

        _db.FlightFollows.Remove(follow);
        await _db.SaveChangesAsync();
        return true;
    }
}
