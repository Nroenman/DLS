namespace AirportSystem.Flights.GraphQL.Inputs.Gates;

public record CreateGateInput(
    string GateNumber,
    string Terminal
);

public record UpdateGateInput(
    Guid Id,
    string? GateNumber = null,
    string? Terminal = null,
    bool? IsAvailable = null
);

public record DeleteGateInput(
    Guid Id
);

public record AssignGateInput(
    Guid GateId,
    Guid FlightId
);

public record ReleaseGateInput(
    Guid FlightId
);
