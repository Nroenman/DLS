using AirportSystem.Flights.Models;

namespace AirportSystem.Flights.Services.Messaging;

public interface IFlightEventPublisher
{
    void PublishFlightUpdated(Flight flight);
}
