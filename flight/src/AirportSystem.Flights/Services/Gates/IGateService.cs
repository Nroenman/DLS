using AirportSystem.Flights.Models;

namespace AirportSystem.Flights.Services.Gates;

public interface IGateService
{
    Task<Gate> CreateGateAsync(string gateNumber, string terminal);
    Task<Gate> UpdateGateAsync(Guid id, string? gateNumber = null, string? terminal = null, bool? isAvailable = null);
    Task<bool> DeleteGateAsync(Guid id);
    Task<Gate> GetGateByIdAsync(Guid id);
    Task<IReadOnlyList<Gate>> GetAllGatesAsync(bool? availableOnly = null);
    Task<Gate> AssignFlightToGateAsync(Guid gateId, Guid flightId);
    Task<Gate> ReleaseGateFromFlightAsync(Guid flightId);
}
