using AirportSystem.Flights.Extensions;
using AirportSystem.Flights.Models;
using AirportSystem.Flights.Services.Auth;
using AirportSystem.Flights.Services.Flights;
using AirportSystem.Flights.Services.Gates;
using HotChocolate.Authorization;

namespace AirportSystem.Flights.GraphQL;

public class Query
{
    // ── Flights ───────────────────────────────────────────────────────────────

    [GraphQLDescription("Retrieve all flights, optionally filtered by direction or status.")]
    [UseFiltering]
    [UseSorting]
    public async Task<IReadOnlyList<Flight>> GetFlights(
        FlightDirection? direction,
        FlightStatus? status,
        [Service] IFlightService flightService)
        => await flightService.GetAllFlightsAsync(direction, status);

    [GraphQLDescription("Retrieve a single flight by its ID.")]
    public async Task<Flight> GetFlight(
        Guid id,
        [Service] IFlightService flightService)
        => await flightService.GetFlightByIdAsync(id);

    [Authorize]
    [GraphQLDescription("Retrieve all flights the currently logged-in user is following.")]
    public async Task<IReadOnlyList<Flight>> GetMyFollowedFlights(
        [Service] IFlightService flightService,
        [Service] IHttpContextAccessor httpContextAccessor)
    {
        var userId = httpContextAccessor.HttpContext!.User.GetUserId();
        return await flightService.GetFollowedFlightsByUserAsync(userId);
    }

    // ── Gates ─────────────────────────────────────────────────────────────────

    [GraphQLDescription("Retrieve all gates, optionally filtered to only available ones.")]
    public async Task<IReadOnlyList<Gate>> GetGates(
        bool? availableOnly,
        [Service] IGateService gateService)
        => await gateService.GetAllGatesAsync(availableOnly);

    [GraphQLDescription("Retrieve a single gate by its ID.")]
    public async Task<Gate> GetGate(
        Guid id,
        [Service] IGateService gateService)
        => await gateService.GetGateByIdAsync(id);

    // ── Current user ──────────────────────────────────────────────────────────

    [Authorize]
    [GraphQLDescription("Retrieve the currently authenticated user's profile.")]
    public async Task<User> GetMe(
        [Service] IUserSyncService userSync,
        [Service] IHttpContextAccessor httpContextAccessor)
        => await userSync.SyncAsync(httpContextAccessor.HttpContext!.User);
}
