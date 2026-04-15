using AirportSystem.Flights.Models;
using Microsoft.EntityFrameworkCore;

namespace AirportSystem.Flights.Data;

/// <summary>
/// Seeds deterministic development data, equivalent to Django's loaddata.
/// Runs on startup when ASPNETCORE_ENVIRONMENT=Development and the database
/// is empty. Safe to re-run — all inserts are skipped if data already exists.
/// </summary>
public static class DevDataSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await SeedGatesAsync(db);
        await SeedFlightsAsync(db);
    }

    private static async Task SeedGatesAsync(AppDbContext db)
    {
        if (await db.Gates.AnyAsync()) return;

        var gates = new[]
        {
            new Gate { Id = GateId("A1"), GateNumber = "A1", Terminal = "A" },
            new Gate { Id = GateId("A2"), GateNumber = "A2", Terminal = "A" },
            new Gate { Id = GateId("A3"), GateNumber = "A3", Terminal = "A" },
            new Gate { Id = GateId("B1"), GateNumber = "B1", Terminal = "B" },
            new Gate { Id = GateId("B2"), GateNumber = "B2", Terminal = "B" },
            new Gate { Id = GateId("C1"), GateNumber = "C1", Terminal = "C", IsAvailable = false },
        };

        db.Gates.AddRange(gates);
        await db.SaveChangesAsync();
    }

    private static async Task SeedFlightsAsync(AppDbContext db)
    {
        if (await db.Flights.AnyAsync()) return;

        var now = DateTime.UtcNow;

        var flights = new[]
        {
            // Departures
            new Flight
            {
                FlightNumber = "BA2490", Airline = "British Airways",
                Origin = "London Heathrow (LHR)", Destination = "Amsterdam (AMS)",
                ScheduledDeparture = now.AddHours(1),  ScheduledArrival = now.AddHours(2.5),
                Direction = FlightDirection.Departure,  Status = FlightStatus.Boarding,
                GateId = GateId("A1")
            },
            new Flight
            {
                FlightNumber = "LH4481", Airline = "Lufthansa",
                Origin = "London Heathrow (LHR)", Destination = "Frankfurt (FRA)",
                ScheduledDeparture = now.AddHours(2),  ScheduledArrival = now.AddHours(4),
                Direction = FlightDirection.Departure,  Status = FlightStatus.Scheduled,
                GateId = GateId("A2")
            },
            new Flight
            {
                FlightNumber = "FR1234", Airline = "Ryanair",
                Origin = "London Heathrow (LHR)", Destination = "Barcelona (BCN)",
                ScheduledDeparture = now.AddHours(3),  ScheduledArrival = now.AddHours(5.5),
                Direction = FlightDirection.Departure,  Status = FlightStatus.Delayed,
                DelayReason = "Late incoming aircraft",
                GateId = GateId("B1")
            },
            new Flight
            {
                FlightNumber = "EK007", Airline = "Emirates",
                Origin = "London Heathrow (LHR)", Destination = "Dubai (DXB)",
                ScheduledDeparture = now.AddHours(5), ScheduledArrival = now.AddHours(12),
                Direction = FlightDirection.Departure, Status = FlightStatus.Scheduled,
            },
            // Arrivals
            new Flight
            {
                FlightNumber = "AF1680", Airline = "Air France",
                Origin = "Paris CDG (CDG)", Destination = "London Heathrow (LHR)",
                ScheduledDeparture = now.AddMinutes(-90), ScheduledArrival = now.AddMinutes(30),
                Direction = FlightDirection.Arrival,   Status = FlightStatus.Scheduled,
                GateId = GateId("A3")
            },
            new Flight
            {
                FlightNumber = "KL1009", Airline = "KLM",
                Origin = "Amsterdam (AMS)", Destination = "London Heathrow (LHR)",
                ScheduledDeparture = now.AddMinutes(-120), ScheduledArrival = now.AddMinutes(-15),
                ActualArrival = now.AddMinutes(-10),
                Direction = FlightDirection.Arrival, Status = FlightStatus.Arrived,
                GateId = GateId("B2")
            },
            new Flight
            {
                FlightNumber = "IB3170", Airline = "Iberia",
                Origin = "Madrid (MAD)", Destination = "London Heathrow (LHR)",
                ScheduledDeparture = now.AddHours(-3), ScheduledArrival = now.AddHours(-1),
                Direction = FlightDirection.Arrival, Status = FlightStatus.Cancelled,
                DelayReason = "Crew unavailability"
            },
        };

        db.Flights.AddRange(flights);
        await db.SaveChangesAsync();
    }

    // Produces a stable, deterministic GUID from a short seed string so
    // fixture IDs are consistent across reseeds (like Django's pk field).
    private static Guid GateId(string seed) =>
        new(MD5Hash(seed));

    private static byte[] MD5Hash(string input)
    {
        var bytes = System.Security.Cryptography.MD5.HashData(
            System.Text.Encoding.UTF8.GetBytes(input));
        return bytes; // 16 bytes → valid Guid
    }
}
