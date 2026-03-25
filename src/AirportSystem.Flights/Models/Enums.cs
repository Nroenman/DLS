namespace AirportSystem.Flights.Models;

public enum FlightStatus
{
    Scheduled,
    Boarding,
    Departed,
    Arrived,
    Delayed,
    Cancelled
}

public enum FlightDirection
{
    Departure,
    Arrival
}

public enum UserRole
{
    Passenger,
    Staff,
    Admin
}
