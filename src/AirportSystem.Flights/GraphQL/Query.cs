using AirportSystem.Flights.Data;
using AirportSystem.Flights.Extensions;
using AirportSystem.Flights.Models;
using AirportSystem.Flights.Services.Flights;
using AirportSystem.Flights.Services.Gates;
using HotChocolate.Authorization;
using Microsoft.EntityFrameworkCore;

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
    [GraphQLDescription("Retrieve all flights the currently logged-in user has booked.")]
    public async Task<IReadOnlyList<Flight>> GetMyBookedFlights(
        [Service] IFlightService flightService,
        [Service] IHttpContextAccessor httpContextAccessor)
    {
        var userId = httpContextAccessor.HttpContext!.User.GetUserId();
        return await flightService.GetBookedFlightsByUserAsync(userId);
    }

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

    // ── Users (Admin only) ────────────────────────────────────────────────────

    [Authorize(Roles = new[] { "Admin" })]
    [GraphQLDescription("(Admin) Retrieve all registered users.")]
    [UseFiltering]
    [UseSorting]
    public IQueryable<User> GetUsers([Service] AppDbContext db)
        => db.Users;

    [Authorize(Roles = new[] { "Admin" })]
    [GraphQLDescription("(Admin) Retrieve a specific user by ID.")]
    public async Task<User?> GetUser(Guid id, [Service] AppDbContext db)
        => await db.Users.FindAsync(id);

    [Authorize]
    [GraphQLDescription("Retrieve the currently authenticated user's profile.")]
    public async Task<User> GetMe(
        [Service] AppDbContext db,
        [Service] IHttpContextAccessor httpContextAccessor)
    {
        var userId = httpContextAccessor.HttpContext!.User.GetUserId();
        return await db.Users.FindAsync(userId)
            ?? throw new UnauthorizedAccessException("User not found.");
    }
}
