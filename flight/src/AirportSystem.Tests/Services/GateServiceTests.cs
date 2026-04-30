using AirportSystem.Flights.Models;
using AirportSystem.Flights.Services.Gates;
using AirportSystem.Tests.Helpers;
using FluentAssertions;

namespace AirportSystem.Tests.Services;

public class GateServiceTests
{
    private static (GateService service, TestDataBuilder seed) Setup()
    {
        var db      = DbContextFactory.Create();
        var seed    = new TestDataBuilder(db);
        var service = new GateService(db);
        return (service, seed);
    }

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateGate_ValidInput_ReturnsGate()
    {
        var (service, _) = Setup();

        var gate = await service.CreateGateAsync("A1", "A");

        gate.Should().NotBeNull();
        gate.GateNumber.Should().Be("A1");
        gate.Terminal.Should().Be("A");
        gate.IsAvailable.Should().BeTrue();
    }

    [Fact]
    public async Task CreateGate_DuplicateGateNumber_ThrowsInvalidOperationException()
    {
        var (service, _) = Setup();
        await service.CreateGateAsync("A1", "A");

        var act = async () => await service.CreateGateAsync("A1", "B");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateGate_ChangeTerminal_Persists()
    {
        var (service, seed) = Setup();
        var gate = seed.SeedGate("C5", "C");

        var updated = await service.UpdateGateAsync(gate.Id, terminal: "D");

        updated.Terminal.Should().Be("D");
        updated.GateNumber.Should().Be("C5"); // unchanged
    }

    [Fact]
    public async Task UpdateGate_SetUnavailable_Persists()
    {
        var (service, seed) = Setup();
        var gate = seed.SeedGate("B2", "B");

        var updated = await service.UpdateGateAsync(gate.Id, isAvailable: false);

        updated.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateGate_DuplicateGateNumber_ThrowsInvalidOperationException()
    {
        var (service, seed) = Setup();
        seed.SeedGate("A1", "A");
        var gate2 = seed.SeedGate("A2", "A");

        var act = async () => await service.UpdateGateAsync(gate2.Id, gateNumber: "A1");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already in use*");
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteGate_NoAssignedFlights_ReturnsTrue()
    {
        var (service, seed) = Setup();
        var gate = seed.SeedGate("D9", "D");

        var result = await service.DeleteGateAsync(gate.Id);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteGate_WithAssignedFlight_ThrowsInvalidOperationException()
    {
        var (service, seed) = Setup();
        var gate   = seed.SeedGate("E1", "E");
        seed.SeedFlight(gateId: gate.Id);

        var act = async () => await service.DeleteGateAsync(gate.Id);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*assigned flights*");
    }

    [Fact]
    public async Task DeleteGate_NonExistentId_ReturnsFalse()
    {
        var (service, _) = Setup();

        var result = await service.DeleteGateAsync(Guid.NewGuid());

        result.Should().BeFalse();
    }

    // ── Assign / Release ──────────────────────────────────────────────────────

    [Fact]
    public async Task AssignFlightToGate_AvailableGate_UpdatesBothEntities()
    {
        var (service, seed) = Setup();
        var gate   = seed.SeedGate("F3", "F");
        var flight = seed.SeedFlight();

        var updatedGate = await service.AssignFlightToGateAsync(gate.Id, flight.Id);

        updatedGate.IsAvailable.Should().BeFalse();
        updatedGate.Flights.Should().Contain(f => f.Id == flight.Id);
    }

    [Fact]
    public async Task AssignFlightToGate_UnavailableGate_ThrowsInvalidOperationException()
    {
        var (service, seed) = Setup();
        var gate    = seed.SeedGate("G1", "G");
        var flight1 = seed.SeedFlight("SK001");
        var flight2 = seed.SeedFlight("SK002");

        // First assignment makes gate unavailable
        await service.AssignFlightToGateAsync(gate.Id, flight1.Id);

        var act = async () => await service.AssignFlightToGateAsync(gate.Id, flight2.Id);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not available*");
    }

    [Fact]
    public async Task ReleaseGate_AssignedFlight_MakesGateAvailableAgain()
    {
        var (service, seed) = Setup();
        var gate   = seed.SeedGate("H2", "H");
        var flight = seed.SeedFlight();
        await service.AssignFlightToGateAsync(gate.Id, flight.Id);

        var releasedGate = await service.ReleaseGateFromFlightAsync(flight.Id);

        releasedGate.IsAvailable.Should().BeTrue();
    }

    [Fact]
    public async Task ReleaseGate_FlightWithNoGate_ThrowsInvalidOperationException()
    {
        var (service, seed) = Setup();
        var flight = seed.SeedFlight(); // no gate assigned

        var act = async () => await service.ReleaseGateFromFlightAsync(flight.Id);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*does not have an assigned gate*");
    }

    // ── Queries ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllGates_AvailableOnlyFilter_ReturnsOnlyAvailable()
    {
        var (service, seed) = Setup();
        seed.SeedGate("J1", "J");                       // available
        var gate2   = seed.SeedGate("J2", "J");         // will be made unavailable
        var flight  = seed.SeedFlight();
        await service.AssignFlightToGateAsync(gate2.Id, flight.Id);

        var available = await service.GetAllGatesAsync(availableOnly: true);

        available.Should().OnlyContain(g => g.IsAvailable);
    }

    [Fact]
    public async Task GetGate_NonExistentId_ThrowsKeyNotFoundException()
    {
        var (service, _) = Setup();

        var act = async () => await service.GetGateByIdAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Gate*not found*");
    }
}
