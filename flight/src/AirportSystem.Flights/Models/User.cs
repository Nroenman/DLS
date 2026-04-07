namespace AirportSystem.Flights.Models;

public class User
{
    /// <summary>
    /// Keycloak's "sub" UUID — used directly as our primary key so no
    /// separate mapping column is needed.
    /// </summary>
    public Guid Id { get; set; }

    public string Username { get; set; } = string.Empty;   // preferred_username
    public string Email { get; set; } = string.Empty;       // email claim
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Passenger;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<FlightFollow> FollowedFlights { get; set; } = new List<FlightFollow>();
}
