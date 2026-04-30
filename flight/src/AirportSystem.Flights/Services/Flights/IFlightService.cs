using AirportSystem.Flights.Models;

namespace AirportSystem.Flights.Services.Flights;

public interface IFlightService
{
    Task<Flight> CreateFlightAsync(
        string flightNumber, string airline,
        string origin, string destination,
        DateTime scheduledDeparture, DateTime scheduledArrival,
        FlightDirection direction, Guid? gateId = null);

    Task<Flight> UpdateFlightAsync(
        Guid id,
        FlightStatus? status = null,
        DateTime? actualDeparture = null,
        DateTime? actualArrival = null,
        string? delayReason = null,
        Guid? gateId = null);

    Task<Flight> GetFlightByIdAsync(Guid id);
    Task<IReadOnlyList<Flight>> GetAllFlightsAsync(FlightDirection? direction = null, FlightStatus? status = null);
    Task<IReadOnlyList<Flight>> GetFollowedFlightsByUserAsync(Guid userId);

    Task<FlightFollow> FollowFlightAsync(Guid userId, Guid flightId);
    Task<bool> UnfollowFlightAsync(Guid userId, Guid flightId);
}
