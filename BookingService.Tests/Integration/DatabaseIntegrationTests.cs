using BookingService.Data;
using BookingService.Models;
using BookingService.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace BookingService.Tests.IntegrationTests;

public class BookingDatabaseIntegrationTests : IAsyncLifetime
{
    private BookingDbContext _context;
    private BookingReadRepository _readRepository;
    private BookingWriteRepository _writeRepository;

    public async Task InitializeAsync()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        var options = new DbContextOptionsBuilder<BookingDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        _context = new BookingDbContext(options);
        _readRepository = new BookingReadRepository(_context);
        _writeRepository = new BookingWriteRepository(_context);
        
        await _context.Database.MigrateAsync();

        await _context.Database.ExecuteSqlRawAsync("DELETE FROM \"Passengers\"");
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM \"Bookings\"");
    }

    public async Task DisposeAsync()
    {
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM \"Passengers\"");
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM \"Bookings\"");
        
        await _context.DisposeAsync();
    }
    
    private Booking CreateTestBooking()
    {
        var bookingId = Guid.NewGuid();
        return new Booking
        {
            Id = bookingId,
            UserId = "testuser",
            FlightId = "Flight123",
            ReturnFlightId = null,
            IsOneWay = true,
            SeatClass = SeatClass.Economy,
            TotalPrice = 1000,
            Status = BookingStatus.Pending,
            ContactEmail = "test@test.com",
            ContactPhone = "12345678",
            CreatedAt = DateTime.UtcNow,
            Passengers = new List<Passenger>
            {
                new Passenger
                {
                    Id = Guid.NewGuid(),
                    BookingId = bookingId,
                    FirstName = "Kader",
                    LastName = "Kivrak",
                    DateOfBirth = new DateTime(1993, 2, 4, 0, 0, 0, DateTimeKind.Utc),
                    PassportNumber = "Pass12345",
                    Nationality = "Danish",
                    SeatNumber = null,
                    IsLeadPassenger = true,
                    HasExtraBaggage = false
                }
            }
        };
    }

    [Fact]
    public async Task AddAsync_WithValidBooking_SavesBookingToDatabase()
    {
        var booking = CreateTestBooking();
        await _writeRepository.AddAsync(booking);

        var result = await _readRepository.GetByIdAsync(booking.Id);

        Assert.NotNull(result);
        Assert.Equal(booking.Id, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsBookingWithPassengers()
    {
        var booking = CreateTestBooking();
        await  _writeRepository.AddAsync(booking);
        
        var result = await _readRepository.GetByIdAsync(booking.Id);
        
        Assert.NotNull(result);
        Assert.Equal(booking.Id, result.Id);
        Assert.NotEmpty(result.Passengers);
    }
    
    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
    {
        var invalidId = Guid.NewGuid();
        var result = await _readRepository.GetByIdAsync(invalidId);
        
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByUserIdAsync_WithValidUserId_ReturnsBookings()
    {
        var booking = CreateTestBooking();
        await  _writeRepository.AddAsync(booking);
        
        var result = await _readRepository.GetByUserIdAsync(booking.UserId);
        
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Equal(booking.UserId, result.First().UserId);
    }

    [Fact]
    public async Task UpdateStatusAsync_WithValidId_UpdatesBookingStatus()
    {
        var booking = CreateTestBooking();
        await _writeRepository.AddAsync(booking);
        await _writeRepository.UpdateStatusAsync(booking.Id, BookingStatus.Confirmed);

        var result = await _readRepository.GetByIdAsync(booking.Id);

        Assert.NotNull(result);
        Assert.Equal(BookingStatus.Confirmed, result.Status);
    }
}