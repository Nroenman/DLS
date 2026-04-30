using AirportSystem.Flights.Data;
using Microsoft.EntityFrameworkCore;

namespace AirportSystem.Tests.Helpers;

/// <summary>
/// Creates a fresh in-memory AppDbContext for each test to ensure isolation.
/// </summary>
public static class DbContextFactory
{
    public static AppDbContext Create(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}
