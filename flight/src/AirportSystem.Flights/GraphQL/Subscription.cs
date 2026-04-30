using AirportSystem.Flights.Models;
using HotChocolate.Authorization;

namespace AirportSystem.Flights.GraphQL;

public class Subscription
{
    /// <summary>
    /// Fires whenever a new flight is created. Available to all users.
    /// </summary>
    [Subscribe]
    [Topic(nameof(OnFlightCreated))]
    [GraphQLDescription("Subscribe to new flight creation events.")]
    public Flight OnFlightCreated([EventMessage] Flight flight) => flight;

    /// <summary>
    /// Fires whenever the specific flight (by id) is updated.
    /// Authenticated users can subscribe to any flight they want to track.
    /// </summary>
    [Authorize]
    [Subscribe]
    [Topic($"{nameof(OnFlightUpdated)}_{{flightId}}")]
    [GraphQLDescription("Subscribe to status/gate/time updates for a specific flight.")]
    public Flight OnFlightUpdated(
        Guid flightId,
        [EventMessage] Flight flight) => flight;
}
