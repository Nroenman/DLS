using AirportSystem.Flights.Models;

namespace AirportSystem.Flights.GraphQL.Inputs.Flights;

public record CreateFlightInput(
    string FlightNumber,
    string Airline,
    string Origin,
    string Destination,
    DateTime ScheduledDeparture,
    DateTime ScheduledArrival,
    FlightDirection Direction,
    Guid? GateId = null
);

public record UpdateFlightInput(
    Guid Id,
    FlightStatus? Status = null,
    DateTime? ActualDeparture = null,
    DateTime? ActualArrival = null,
    string? DelayReason = null,
    Guid? GateId = null
);

public record FollowFlightInput(
    Guid FlightId
);

public record UnfollowFlightInput(
    Guid FlightId
);
