using AirportSystem.Flights.Data;
using AirportSystem.Flights.Models;

namespace AirportSystem.Flights.GraphQL.Types;

public class UserType : ObjectType<User>
{
    protected override void Configure(IObjectTypeDescriptor<User> descriptor)
    {
        descriptor.Description("An airport system user (Passenger, Staff, or Admin).");

        // Never expose password hash
        descriptor.Field(u => u.PasswordHash).Ignore();

        descriptor
            .Field("bookedFlights")
            .Description("All flights the user is booked on.")
            .ResolveWith<UserResolvers>(r => r.GetBookedFlights(default!, default!));

        descriptor
            .Field("followedFlights")
            .Description("All flights the user is currently following.")
            .ResolveWith<UserResolvers>(r => r.GetFollowedFlights(default!, default!));
    }
}

public class UserResolvers
{
    public IQueryable<Flight> GetBookedFlights(
        [Parent] User user,
        AppDbContext db)
        => db.Bookings
             .Where(b => b.UserId == user.Id)
             .Select(b => b.Flight);

    public IQueryable<Flight> GetFollowedFlights(
        [Parent] User user,
        AppDbContext db)
        => db.FlightFollows
             .Where(ff => ff.UserId == user.Id)
             .Select(ff => ff.Flight);
}

public class FlightType : ObjectType<Flight>
{
    protected override void Configure(IObjectTypeDescriptor<Flight> descriptor)
    {
        descriptor.Description("Represents a departure or arrival flight.");

        descriptor
            .Field(f => f.Gate)
            .Description("The gate assigned to this flight, if any.")
            .ResolveWith<FlightResolvers>(r => r.GetGate(default!, default!));
    }
}

public class FlightResolvers
{
    public Gate? GetGate([Parent] Flight flight, AppDbContext db)
        => flight.GateId.HasValue
            ? db.Gates.Find(flight.GateId.Value)
            : null;
}

public class GateType : ObjectType<Gate>
{
    protected override void Configure(IObjectTypeDescriptor<Gate> descriptor)
    {
        descriptor.Description("An airport gate that can be assigned to flights.");

        descriptor
            .Field(g => g.Flights)
            .Description("Flights currently or previously assigned to this gate.")
            .ResolveWith<GateResolvers>(r => r.GetFlights(default!, default!));
    }
}

public class GateResolvers
{
    public IQueryable<Flight> GetFlights([Parent] Gate gate, AppDbContext db)
        => db.Flights.Where(f => f.GateId == gate.Id);
}
