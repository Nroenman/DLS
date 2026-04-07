namespace AirportSystem.Flights.GraphQL.Inputs.Auth;

public record RegisterInput(
    string Username,
    string Email,
    string Password,
    /// <summary>Realm role to assign. Valid values: Passenger (default), Staff, Admin.</summary>
    string Role = "Passenger"
);

public record LoginInput(
    string Email,
    string Password
);
