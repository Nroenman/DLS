using AirportSystem.Flights.Models;
using Microsoft.EntityFrameworkCore;

namespace AirportSystem.Flights.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Flight> Flights => Set<Flight>();
    public DbSet<Gate> Gates => Set<Gate>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<FlightFollow> FlightFollows => Set<FlightFollow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── User ──────────────────────────────────────────────────────────────
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            // Id is set externally from Keycloak sub — do NOT auto-generate
            e.Property(u => u.Id).ValueGeneratedNever();
            e.HasIndex(u => u.Email).IsUnique();
            e.HasIndex(u => u.Username).IsUnique();
            e.Property(u => u.Username).HasMaxLength(50).IsRequired();
            e.Property(u => u.Email).HasMaxLength(200).IsRequired();
            e.Property(u => u.Role).HasConversion<string>();
        });

        // ── Gate ──────────────────────────────────────────────────────────────
        modelBuilder.Entity<Gate>(e =>
        {
            e.HasKey(g => g.Id);
            e.HasIndex(g => g.GateNumber).IsUnique();
            e.Property(g => g.GateNumber).HasMaxLength(10).IsRequired();
            e.Property(g => g.Terminal).HasMaxLength(10).IsRequired();
        });

        // ── Flight ────────────────────────────────────────────────────────────
        modelBuilder.Entity<Flight>(e =>
        {
            e.HasKey(f => f.Id);
            e.HasIndex(f => f.FlightNumber);
            e.Property(f => f.FlightNumber).HasMaxLength(20).IsRequired();
            e.Property(f => f.Airline).HasMaxLength(100).IsRequired();
            e.Property(f => f.Origin).HasMaxLength(100).IsRequired();
            e.Property(f => f.Destination).HasMaxLength(100).IsRequired();
            e.Property(f => f.Status).HasConversion<string>();
            e.Property(f => f.Direction).HasConversion<string>();

            e.HasOne(f => f.Gate)
             .WithMany(g => g.Flights)
             .HasForeignKey(f => f.GateId)
             .OnDelete(DeleteBehavior.SetNull)
             .IsRequired(false);
        });

        // ── Booking ───────────────────────────────────────────────────────────
        modelBuilder.Entity<Booking>(e =>
        {
            e.HasKey(b => b.Id);
            e.HasIndex(b => new { b.UserId, b.FlightId }).IsUnique();

            e.HasOne(b => b.User)
             .WithMany(u => u.Bookings)
             .HasForeignKey(b => b.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(b => b.Flight)
             .WithMany(f => f.Bookings)
             .HasForeignKey(b => b.FlightId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── FlightFollow ──────────────────────────────────────────────────────
        modelBuilder.Entity<FlightFollow>(e =>
        {
            e.HasKey(ff => ff.Id);
            e.HasIndex(ff => new { ff.UserId, ff.FlightId }).IsUnique();

            e.HasOne(ff => ff.User)
             .WithMany(u => u.FollowedFlights)
             .HasForeignKey(ff => ff.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(ff => ff.Flight)
             .WithMany(f => f.Followers)
             .HasForeignKey(ff => ff.FlightId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
