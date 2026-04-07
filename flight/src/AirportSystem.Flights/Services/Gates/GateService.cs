using AirportSystem.Flights.Data;
using AirportSystem.Flights.Models;
using Microsoft.EntityFrameworkCore;

namespace AirportSystem.Flights.Services.Gates;

public class GateService : IGateService
{
    private readonly AppDbContext _db;

    public GateService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Gate> CreateGateAsync(string gateNumber, string terminal)
    {
        if (await _db.Gates.AnyAsync(g => g.GateNumber == gateNumber))
            throw new InvalidOperationException($"Gate '{gateNumber}' already exists.");

        var gate = new Gate { GateNumber = gateNumber, Terminal = terminal };
        _db.Gates.Add(gate);
        await _db.SaveChangesAsync();
        return gate;
    }

    public async Task<Gate> UpdateGateAsync(
        Guid id,
        string? gateNumber = null,
        string? terminal = null,
        bool? isAvailable = null)
    {
        var gate = await _db.Gates.FindAsync(id)
            ?? throw new KeyNotFoundException($"Gate '{id}' not found.");

        if (gateNumber is not null)
        {
            if (await _db.Gates.AnyAsync(g => g.GateNumber == gateNumber && g.Id != id))
                throw new InvalidOperationException($"Gate number '{gateNumber}' is already in use.");
            gate.GateNumber = gateNumber;
        }

        if (terminal is not null)
            gate.Terminal = terminal;

        if (isAvailable.HasValue)
            gate.IsAvailable = isAvailable.Value;

        await _db.SaveChangesAsync();
        return await GetGateByIdAsync(gate.Id);
    }

    public async Task<bool> DeleteGateAsync(Guid id)
    {
        var gate = await _db.Gates.FindAsync(id);
        if (gate is null) return false;

        // Ensure no flights are currently assigned
        var hasFlights = await _db.Flights.AnyAsync(f => f.GateId == id);
        if (hasFlights)
            throw new InvalidOperationException(
                "Cannot delete a gate that has assigned flights. Reassign them first.");

        _db.Gates.Remove(gate);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<Gate> GetGateByIdAsync(Guid id)
    {
        return await _db.Gates
            .Include(g => g.Flights)
            .FirstOrDefaultAsync(g => g.Id == id)
            ?? throw new KeyNotFoundException($"Gate '{id}' not found.");
    }

    public async Task<IReadOnlyList<Gate>> GetAllGatesAsync(bool? availableOnly = null)
    {
        var query = _db.Gates.Include(g => g.Flights).AsQueryable();

        if (availableOnly == true)
            query = query.Where(g => g.IsAvailable);

        return await query.OrderBy(g => g.Terminal).ThenBy(g => g.GateNumber).ToListAsync();
    }

    public async Task<Gate> AssignFlightToGateAsync(Guid gateId, Guid flightId)
    {
        var gate = await _db.Gates.FindAsync(gateId)
            ?? throw new KeyNotFoundException($"Gate '{gateId}' not found.");

        var flight = await _db.Flights.FindAsync(flightId)
            ?? throw new KeyNotFoundException($"Flight '{flightId}' not found.");

        if (!gate.IsAvailable)
            throw new InvalidOperationException($"Gate '{gate.GateNumber}' is not available.");

        flight.GateId = gateId;
        gate.IsAvailable = false;
        flight.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return await GetGateByIdAsync(gateId);
    }

    public async Task<Gate> ReleaseGateFromFlightAsync(Guid flightId)
    {
        var flight = await _db.Flights
            .Include(f => f.Gate)
            .FirstOrDefaultAsync(f => f.Id == flightId)
            ?? throw new KeyNotFoundException($"Flight '{flightId}' not found.");

        if (flight.GateId is null || flight.Gate is null)
            throw new InvalidOperationException("This flight does not have an assigned gate.");

        var gate = flight.Gate;
        flight.GateId = null;
        gate.IsAvailable = true;
        flight.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return await GetGateByIdAsync(gate.Id);
    }
}
