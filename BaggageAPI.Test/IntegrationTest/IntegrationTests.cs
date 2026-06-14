using BaggageAPI.Data;
using BaggageAPI.Dtos;
using BaggageAPI.Interfaces;
using BaggageAPI.Models;
using BaggageAPI.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Testcontainers.PostgreSql;
using Xunit;

namespace BaggageAPI.Test.IntegrationTest;

public class BaggageServiceIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("baggage_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private AppDbContext _ctx = null!;
    private BaggageService _service = null!;
    private Mock<IRabbitMqService> _rabbitMock = null!;

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        _ctx = new AppDbContext(options);
        await _ctx.Database.EnsureCreatedAsync();

        _rabbitMock = new Mock<IRabbitMqService>();
        _service = new BaggageService(_ctx, _rabbitMock.Object);
    }

    public async ValueTask DisposeAsync()
    {
        await _ctx.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    private async Task ResetAsync()
    {
        _ctx.Baggages.RemoveRange(_ctx.Baggages);
        await _ctx.SaveChangesAsync();
    }

    [Fact]
    public async Task CreateAsync_ValidDto_CanBeReadBackFromDatabase()
    {
        await ResetAsync();

        var dto = new CreateBaggageDto
        {
            BookingId   = Guid.NewGuid(),
            PassengerId = Guid.NewGuid(),
            Weight      = 23.5
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
    public async Task CreateAsync_ValidDto_HasCorrectInitialStatusAndLocation()
    {
        await ResetAsync();

        var created = await _service.CreateAsync(new CreateBaggageDto
        {
            BookingId   = Guid.NewGuid(),
            PassengerId = Guid.NewGuid(),
            Weight      = 10.0
        });

        var stored = await _ctx.Baggages.AsNoTracking().FirstAsync(b => b.Id == created.Id);

        Assert.Equal(BaggageStatus.CheckedIn, stored.Status);
        Assert.Equal("Check-in", stored.CurrentLocation);
    }

    [Fact]
    public async Task CreateAsync_MultipleBaggage_EachGetUniqueId()
    {
        await ResetAsync();

        var passengerId = Guid.NewGuid();
        var b1 = await _service.CreateAsync(new CreateBaggageDto { BookingId = Guid.NewGuid(), PassengerId = passengerId, Weight = 5 });
        var b2 = await _service.CreateAsync(new CreateBaggageDto { BookingId = Guid.NewGuid(), PassengerId = passengerId, Weight = 8 });

        Assert.NotEqual(b1.Id, b2.Id);

        var count = await _ctx.Baggages.CountAsync(b => b.PassengerId == passengerId);
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task UpdateStatusAsync_ExistingId_ChangesArePersisted()
    {
        await ResetAsync();

        var created = await _service.CreateAsync(new CreateBaggageDto
        {
            BookingId   = Guid.NewGuid(),
            PassengerId = Guid.NewGuid(),
            Weight      = 15.0
        });

        await _service.UpdateStatusAsync(created.Id, new UpdateBaggageStatusDto
        {
            Status   = BaggageStatus.Loaded,
            Location = "Gate 12"
        });

        var stored = await _ctx.Baggages.AsNoTracking().FirstAsync(b => b.Id == created.Id);

        Assert.Equal(BaggageStatus.Loaded, stored.Status);
        Assert.Equal("Gate 12", stored.CurrentLocation);
    }

    [Fact]
    public async Task UpdateStatusAsync_FullLifecycle_AllStatusesPersistedCorrectly()
    {
        await ResetAsync();

        var created = await _service.CreateAsync(new CreateBaggageDto
        {
            BookingId   = Guid.NewGuid(),
            PassengerId = Guid.NewGuid(),
            Weight      = 18.0
        });

        var transitions = new[]
        {
            (BaggageStatus.Loaded,    "Gate 5"),
            (BaggageStatus.InTransit, "Tarmac"),
            (BaggageStatus.Claimed,   "Carousel 2")
        };

        foreach (var (status, location) in transitions)
        {
            var result = await _service.UpdateStatusAsync(created.Id, new UpdateBaggageStatusDto
            {
                Status   = status,
                Location = location
            });
            Assert.Equal(status,   result!.Status);
            Assert.Equal(location, result.CurrentLocation);
        }

        var final = await _ctx.Baggages.AsNoTracking().FirstAsync(b => b.Id == created.Id);
        Assert.Equal(BaggageStatus.Claimed, final.Status);
        Assert.Equal("Carousel 2", final.CurrentLocation);
    }

    [Fact]
    public async Task UpdateStatusAsync_NonExistentId_ReturnsNullAndDoesNotThrow()
    {
        await ResetAsync();

        var result = await _service.UpdateStatusAsync(Guid.NewGuid(), new UpdateBaggageStatusDto
        {
            Status   = BaggageStatus.InTransit,
            Location = "Anywhere"
        });

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByPassenger_ReturnsOnlyBaggageBelongingToPassenger()
    {
        await ResetAsync();

        var target = Guid.NewGuid();
        var other  = Guid.NewGuid();

        await _service.CreateAsync(new CreateBaggageDto { BookingId = Guid.NewGuid(), PassengerId = target, Weight = 10 });
        await _service.CreateAsync(new CreateBaggageDto { BookingId = Guid.NewGuid(), PassengerId = target, Weight = 12 });
        await _service.CreateAsync(new CreateBaggageDto { BookingId = Guid.NewGuid(), PassengerId = other,  Weight = 8  });

        var result = await _service.GetByPassenger(target);

        Assert.Equal(2, result.Count);
        Assert.All(result, b => Assert.Equal(target, b.PassengerId));
    }

    [Fact]
    public async Task GetByPassenger_UnknownPassengerId_ReturnsEmptyList()
    {
        await ResetAsync();

        var result = await _service.GetByPassenger(Guid.NewGuid());

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByPassenger_ReflectsMostRecentStatusAfterUpdate()
    {
        await ResetAsync();

        var passengerId = Guid.NewGuid();
        var created = await _service.CreateAsync(new CreateBaggageDto
        {
            BookingId   = Guid.NewGuid(),
            PassengerId = passengerId,
            Weight      = 20.0
        });

        await _service.UpdateStatusAsync(created.Id, new UpdateBaggageStatusDto
        {
            Status   = BaggageStatus.Claimed,
            Location = "Carousel 1"
        });

        var result = await _service.GetByPassenger(passengerId);

        var bag = Assert.Single(result);
        Assert.Equal(BaggageStatus.Claimed, bag.Status);
        Assert.Equal("Carousel 1", bag.CurrentLocation);
    }
}