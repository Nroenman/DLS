using Microsoft.EntityFrameworkCore;
using BookingService.Models;

namespace BookingService.Data;

public class BookingDbContext : DbContext
{
    public BookingDbContext(DbContextOptions<BookingDbContext> options) : base(options)
    {
    }
    
    public DbSet<Booking> Bookings { get; set; }
    public DbSet<Passenger> Passengers { get; set; }
    public DbSet<Pricing> Pricings { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Pricing>().HasData(new Pricing
        {
            Id = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890"),
            ExtraBaggageFee = 200,
            ChildDiscount = 0.5m
        });
    }
}