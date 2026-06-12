using BaggageAPI.Data;
using BaggageAPI.Dtos;
using BaggageAPI.Models;
using BaggageAPI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Testcontainers.PostgreSql;
using Xunit;

namespace BaggageAPI.Tests.Unit;

// ── Shared container fixture ───────────────────────────────────────────────────
// One PostgreSQL container is started for the entire unit test class.
// Each test calls ResetAsync() to truncate data, so tests stay isolated
// without the cost of spinning up a new container per test.

public class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("baggage_unit")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public string ConnectionString { get; private set; } = string.Empty;

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();

        // Apply schema once
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        await using var ctx = new AppDbContext(options);
        await ctx.Database.EnsureCreatedAsync();
    }

    public async ValueTask DisposeAsync() => await _container.DisposeAsync();
}

// ── Unit tests ─────────────────────────────────────────────────────────────────
// "Unit" here means: one class under test (BaggageService) with all *external*
// dependencies either real-but-isolated (PostgreSQL via Testcontainers) or
// mocked (RabbitMQ). This gives true SQL behaviour without a live broker.

public class BaggageServiceTests(PostgresFixture fixture)
    : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private AppDbContext _ctx = null!;
    private BaggageService _service = null!;
    private Mock<RabbitMqService> _rabbitMock = null!;

    // ── Per-test setup / teardown ──────────────────────────────────────────────

    public async ValueTask InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;

        _ctx = new AppDbContext(options);

        _rabbitMock = new Mock<RabbitMqService>(
            MockBehavior.Loose, Mock.Of<IConfiguration>());
        _rabbitMock.Setup(r => r.Publish(It.IsAny<string>(), It.IsAny<object>()));

        _service = new BaggageService(_ctx, _rabbitMock.Object);

        // Clean slate before every test
        await TruncateAsync();
    }

    public async ValueTask DisposeAsync() => await _ctx.DisposeAsync();

    private async Task TruncateAsync()
    {
        await _ctx.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"Baggages\" RESTART IDENTITY CASCADE;");
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static Baggage SeedBaggage(Guid? passengerId = null) => new()
    {
        Id              = Guid.NewGuid(),
        BookingId       = Guid.NewGuid(),
        PassengerId     = passengerId ?? Guid.NewGuid(),
        Weight          = 10.0,
        Status          = BaggageStatus.CheckedIn,
        CurrentLocation = "Check-in",
        CreatedAt       = DateTime.UtcNow
    };

    // ── CreateAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsBaggageWithCheckedInStatus()
    {
        // White-box: BaggageService hardcodes Status = BaggageStatus.CheckedIn
        var result = await _service.CreateAsync(new CreateBaggageDto
        {
            BookingId   = Guid.NewGuid(),
            PassengerId = Guid.NewGuid(),
            Weight      = 23.5
        });

        Assert.Equal(BaggageStatus.CheckedIn, result.Status);
    }

    [Fact]
    public async Task CreateAsync_ValidDto_SetsCurrentLocationToCheckIn()
    {
        // White-box: service hardcodes CurrentLocation = "Check-in"
        var result = await _service.CreateAsync(new CreateBaggageDto
        {
            BookingId   = Guid.NewGuid(),
            PassengerId = Guid.NewGuid(),
            Weight      = 10.0
        });

        Assert.Equal("Check-in", result.CurrentLocation);
    }

    [Fact]
    public async Task CreateAsync_ValidDto_AssignsNonEmptyGuid()
    {
        var result = await _service.CreateAsync(new CreateBaggageDto
        {
            BookingId   = Guid.NewGuid(),
            PassengerId = Guid.NewGuid(),
            Weight      = 8.0
        });

        Assert.NotEqual(Guid.Empty, result.Id);
    }

    [Fact]
    public async Task CreateAsync_ValidDto_PersistsBaggageToPostgres()
    {
        // Re-query with AsNoTracking to confirm a real round-trip to PostgreSQL,
        // not just EF's change tracker returning the cached object.
        var dto = new CreateBaggageDto
        {
            BookingId   = Guid.NewGuid(),
            PassengerId = Guid.NewGuid(),
            Weight      = 15.0
        };

        var created = await _service.CreateAsync(dto);

        var stored = await _ctx.Baggages
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == created.Id);

        Assert.NotNull(stored);
        Assert.Equal(dto.BookingId,   stored!.BookingId);
        Assert.Equal(dto.PassengerId, stored.PassengerId);
        Assert.Equal(dto.Weight,      stored.Weight);
    }

    [Fact]
    public async Task CreateAsync_ValidDto_SetsCreatedAtToUtcNow()
    {
        var before = DateTime.UtcNow;

        var result = await _service.CreateAsync(new CreateBaggageDto
        {
            BookingId   = Guid.NewGuid(),
            PassengerId = Guid.NewGuid(),
            Weight      = 5.0
        });

        var after = DateTime.UtcNow;

        Assert.InRange(result.CreatedAt, before, after);
    }

    [Fact]
    public async Task CreateAsync_ValidDto_PublishesToBaggageQueue()
    {
        // White-box: Publish("baggagequeue", ...) is called exactly once after save
        await _service.CreateAsync(new CreateBaggageDto
        {
            BookingId   = Guid.NewGuid(),
            PassengerId = Guid.NewGuid(),
            Weight      = 20.0
        });

        _rabbitMock.Verify(
            r => r.Publish("baggagequeue", It.IsAny<object>()),
            Times.Once);
    }

    // ── UpdateStatusAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateStatusAsync_ExistingId_UpdatesStatusAndLocation()
    {
        var baggage = SeedBaggage();
        _ctx.Baggages.Add(baggage);
        await _ctx.SaveChangesAsync();

        var result = await _service.UpdateStatusAsync(baggage.Id, new UpdateBaggageStatusDto
        {
            Status   = BaggageStatus.Loaded,
            Location = "Gate 7"
        });

        Assert.NotNull(result);
        Assert.Equal(BaggageStatus.Loaded, result!.Status);
        Assert.Equal("Gate 7", result.CurrentLocation);
    }

    [Fact]
    public async Task UpdateStatusAsync_ExistingId_PersistsChangesToPostgres()
    {
        var baggage = SeedBaggage();
        _ctx.Baggages.Add(baggage);
        await _ctx.SaveChangesAsync();

        await _service.UpdateStatusAsync(baggage.Id, new UpdateBaggageStatusDto
        {
            Status   = BaggageStatus.Claimed,
            Location = "Carousel 3"
        });

        var stored = await _ctx.Baggages.AsNoTracking().FirstAsync(b => b.Id == baggage.Id);
        Assert.Equal(BaggageStatus.Claimed, stored.Status);
        Assert.Equal("Carousel 3", stored.CurrentLocation);
    }

    [Fact]
    public async Task UpdateStatusAsync_NonExistentId_ReturnsNull()
    {
        // White-box: FindAsync returns null → service returns null without touching DB
        var result = await _service.UpdateStatusAsync(Guid.NewGuid(), new UpdateBaggageStatusDto
        {
            Status   = BaggageStatus.InTransit,
            Location = "Baggage Hall"
        });

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateStatusAsync_ExistingId_PublishesToBaggageQueue()
    {
        // White-box: Publish is called only when baggage is found
        var baggage = SeedBaggage();
        _ctx.Baggages.Add(baggage);
        await _ctx.SaveChangesAsync();

        await _service.UpdateStatusAsync(baggage.Id, new UpdateBaggageStatusDto
        {
            Status   = BaggageStatus.InTransit,
            Location = "Tarmac"
        });

        _rabbitMock.Verify(
            r => r.Publish("baggagequeue", It.IsAny<object>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateStatusAsync_NonExistentId_DoesNotPublish()
    {
        // White-box: null-guard returns before Publish is reached
        await _service.UpdateStatusAsync(Guid.NewGuid(), new UpdateBaggageStatusDto
        {
            Status   = BaggageStatus.Loaded,
            Location = "Somewhere"
        });

        _rabbitMock.Verify(
            r => r.Publish(It.IsAny<string>(), It.IsAny<object>()),
            Times.Never);
    }

    // ── GetByPassenger ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByPassenger_PassengerWithBaggage_ReturnsOnlyTheirBaggage()
    {
        var target = Guid.NewGuid();
        var other  = Guid.NewGuid();

        _ctx.Baggages.AddRange(
            SeedBaggage(target),
            SeedBaggage(target),
            SeedBaggage(other)
        );
        await _ctx.SaveChangesAsync();

        var result = await _service.GetByPassenger(target);

        Assert.Equal(2, result.Count);
        Assert.All(result, b => Assert.Equal(target, b.PassengerId));
    }

    [Fact]
    public async Task GetByPassenger_UnknownPassengerId_ReturnsEmptyList()
    {
        var result = await _service.GetByPassenger(Guid.NewGuid());

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByPassenger_DoesNotReturnOtherPassengersBaggage()
    {
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();

        _ctx.Baggages.AddRange(SeedBaggage(p1), SeedBaggage(p2));
        await _ctx.SaveChangesAsync();

        var result = await _service.GetByPassenger(p1);

        Assert.Single(result);
        Assert.DoesNotContain(result, b => b.PassengerId == p2);
    }
}