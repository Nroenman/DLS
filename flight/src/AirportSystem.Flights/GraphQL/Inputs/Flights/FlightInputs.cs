using AirportSystem.Flights.Models;

namespace AirportSystem.Flights.GraphQL.Inputs.Flights;

public record CreateFlightInput(
    string FlightNumber,
    string Airline,
    string Origin,
    string Destination,
    DateTime ScheduledTime,
    FlightDirection Direction,
    Guid? GateId = null
);

public record UpdateFlightInput(
    Guid Id,
    FlightStatus? Status = null,
    DateTime? ActualTime = null,
    string? DelayReason = null,
    Guid? GateId = null
);

public record FollowFlightInput(
    Guid FlightId
);

public record UnfollowFlightInput(
    Guid FlightId
);
