using AirportSystem.Flights.Models;
using AirportSystem.Flights.Services.Flights;
using AirportSystem.Tests.Helpers;
using FluentAssertions;

namespace AirportSystem.Tests.Services;

public class FlightServiceTests
{
    private static (FlightService service, TestDataBuilder seed, Flights.Data.AppDbContext db) Setup()
    {
        var db      = DbContextFactory.Create();
        var seed    = new TestDataBuilder(db);
        var service = new FlightService(db);
        return (service, seed, db);
    }

    private static DateTime Future(int hours = 2) => DateTime.UtcNow.AddHours(hours);

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateFlight_ValidInput_ReturnsFlight()
    {
        var (service, _, _) = Setup();

        var flight = await service.CreateFlightAsync(
            "SK101", "SAS", "CPH", "LHR",
            Future(2), Future(4), FlightDirection.Departure);

        flight.Should().NotBeNull();
        flight.FlightNumber.Should().Be("SK101");
        flight.Status.Should().Be(FlightStatus.Scheduled);
    }

    [Fact]
    public async Task CreateFlight_ArrivalBeforeDeparture_ThrowsArgumentException()
    {
        var (service, _, _) = Setup();

        var act = async () => await service.CreateFlightAsync(
            "SK202", "SAS", "CPH", "LHR",
            Future(4), Future(2), FlightDirection.Departure);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Arrival must be after departure*");
    }

    [Fact]
    public async Task CreateFlight_WithValidGate_AssignsGate()
    {
        var (service, seed, _) = Setup();
        var gate = seed.SeedGate("B3", "B");

        var flight = await service.CreateFlightAsync(
            "SK303", "SAS", "CPH", "OSL",
            Future(1), Future(3), FlightDirection.Departure, gate.Id);

        flight.GateId.Should().Be(gate.Id);
    }

    [Fact]
    public async Task CreateFlight_WithInvalidGate_ThrowsKeyNotFoundException()
    {
        var (service, _, _) = Setup();

        var act = async () => await service.CreateFlightAsync(
            "SK404", "SAS", "CPH", "AMS",
            Future(1), Future(3), FlightDirection.Departure, Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Gate*not found*");
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateFlight_StatusChange_Persists()
    {
        var (service, seed, _) = Setup();
        var flight = seed.SeedFlight();

        var updated = await service.UpdateFlightAsync(flight.Id, status: FlightStatus.Boarding);

        updated.Status.Should().Be(FlightStatus.Boarding);
    }

    [Fact]
    public async Task UpdateFlight_WithDelayReason_Persists()
    {
        var (service, seed, _) = Setup();
        var flight = seed.SeedFlight();

        var updated = await service.UpdateFlightAsync(
            flight.Id,
            status: FlightStatus.Delayed,
            delayReason: "Technical issue");

        updated.Status.Should().Be(FlightStatus.Delayed);
        updated.DelayReason.Should().Be("Technical issue");
    }

    [Fact]
    public async Task UpdateFlight_NonExistentId_ThrowsKeyNotFoundException()
    {
        var (service, _, _) = Setup();

        var act = async () => await service.UpdateFlightAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Flight*not found*");
    }

    // ── Booking ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task BookFlight_ValidUserAndFlight_CreatesBooking()
    {
        var (service, seed, _) = Setup();
        var user   = seed.SeedUser();
        var flight = seed.SeedFlight();

        var booking = await service.BookFlightAsync(user.Id, flight.Id, "12A");

        booking.Should().NotBeNull();
        booking.UserId.Should().Be(user.Id);
        booking.FlightId.Should().Be(flight.Id);
        booking.SeatNumber.Should().Be("12A");
    }

    [Fact]
    public async Task BookFlight_DuplicateBooking_ThrowsInvalidOperationException()
    {
        var (service, seed, _) = Setup();
        var user   = seed.SeedUser();
        var flight = seed.SeedFlight();

        await service.BookFlightAsync(user.Id, flight.Id);

        var act = async () => await service.BookFlightAsync(user.Id, flight.Id);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already booked*");
    }

    [Fact]
    public async Task UnbookFlight_ExistingBooking_ReturnsTrue()
    {
        var (service, seed, _) = Setup();
        var user   = seed.SeedUser();
        var flight = seed.SeedFlight();
        await service.BookFlightAsync(user.Id, flight.Id);

        var result = await service.UnbookFlightAsync(user.Id, flight.Id);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task UnbookFlight_NoBookingExists_ReturnsFalse()
    {
        var (service, seed, _) = Setup();
        var user   = seed.SeedUser();
        var flight = seed.SeedFlight();

        var result = await service.UnbookFlightAsync(user.Id, flight.Id);

        result.Should().BeFalse();
    }

    // ── Follow ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task FollowFlight_ValidInput_CreatesFollow()
    {
        var (service, seed, _) = Setup();
        var user   = seed.SeedUser();
        var flight = seed.SeedFlight();

        var follow = await service.FollowFlightAsync(user.Id, flight.Id);

        follow.Should().NotBeNull();
        follow.UserId.Should().Be(user.Id);
        follow.FlightId.Should().Be(flight.Id);
    }

    [Fact]
    public async Task FollowFlight_DuplicateFollow_ThrowsInvalidOperationException()
    {
        var (service, seed, _) = Setup();
        var user   = seed.SeedUser();
        var flight = seed.SeedFlight();
        await service.FollowFlightAsync(user.Id, flight.Id);

        var act = async () => await service.FollowFlightAsync(user.Id, flight.Id);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already following*");
    }

    [Fact]
    public async Task UnfollowFlight_ExistingFollow_ReturnsTrue()
    {
        var (service, seed, _) = Setup();
        var user   = seed.SeedUser();
        var flight = seed.SeedFlight();
        await service.FollowFlightAsync(user.Id, flight.Id);

        var result = await service.UnfollowFlightAsync(user.Id, flight.Id);

        result.Should().BeTrue();
    }

    // ── Queries ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllFlights_FilterByDirection_ReturnsOnlyMatching()
    {
        var (service, seed, _) = Setup();
        seed.SeedFlight("SK001", FlightDirection.Departure);
        seed.SeedFlight("SK002", FlightDirection.Arrival);
        seed.SeedFlight("SK003", FlightDirection.Departure);

        var departures = await service.GetAllFlightsAsync(direction: FlightDirection.Departure);

        departures.Should().HaveCount(2);
        departures.Should().OnlyContain(f => f.Direction == FlightDirection.Departure);
    }

    [Fact]
    public async Task GetAllFlights_FilterByStatus_ReturnsOnlyMatching()
    {
        var (service, seed, _) = Setup();
        seed.SeedFlight("SK001", status: FlightStatus.Scheduled);
        seed.SeedFlight("SK002", status: FlightStatus.Delayed);
        seed.SeedFlight("SK003", status: FlightStatus.Delayed);

        var delayed = await service.GetAllFlightsAsync(status: FlightStatus.Delayed);

        delayed.Should().HaveCount(2);
        delayed.Should().OnlyContain(f => f.Status == FlightStatus.Delayed);
    }

    [Fact]
    public async Task GetBookedFlightsByUser_ReturnsOnlyUserFlights()
    {
        var (service, seed, _) = Setup();
        var userA  = seed.SeedUser("userA", "a@test.com");
        var userB  = seed.SeedUser("userB", "b@test.com");
        var flight1 = seed.SeedFlight("SK001");
        var flight2 = seed.SeedFlight("SK002");

        await service.BookFlightAsync(userA.Id, flight1.Id);
        await service.BookFlightAsync(userB.Id, flight2.Id);

        var userAFlights = await service.GetBookedFlightsByUserAsync(userA.Id);

        userAFlights.Should().HaveCount(1);
        userAFlights[0].FlightNumber.Should().Be("SK001");
    }
}
