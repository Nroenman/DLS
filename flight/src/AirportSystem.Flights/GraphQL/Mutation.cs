using AirportSystem.Flights.Extensions;
using AirportSystem.Flights.GraphQL.Inputs.Auth;
using AirportSystem.Flights.GraphQL.Inputs.Flights;
using AirportSystem.Flights.GraphQL.Inputs.Gates;
using AirportSystem.Flights.GraphQL.Payloads;
using AirportSystem.Flights.Services.Auth;
using AirportSystem.Flights.Services.Flights;
using AirportSystem.Flights.Services.Gates;
using HotChocolate.Authorization;
using HotChocolate.Subscriptions;

namespace AirportSystem.Flights.GraphQL;

public class Mutation
{
    // ── Auth ──────────────────────────────────────────────────────────────────

    [GraphQLDescription(
        "Register a new user account. Keycloak creates the user and assigns the requested role.")]
    public async Task<AuthPayload> Register(
        RegisterInput input,
        [Service] IKeycloakService keycloakService)
    {
        var tokenResponse = await keycloakService.RegisterAsync(
            input.Username, input.Email, input.Password, input.Role);

        return new AuthPayload(
            tokenResponse.AccessToken,
            tokenResponse.RefreshToken,
            tokenResponse.ExpiresIn);
    }

    [GraphQLDescription(
        "Log in with email and password. Returns a Keycloak JWT to use as Bearer token.")]
    public async Task<AuthPayload> Login(
        LoginInput input,
        [Service] IKeycloakService keycloakService)
    {
        var tokenResponse = await keycloakService.LoginAsync(input.Email, input.Password);

        return new AuthPayload(
            tokenResponse.AccessToken,
            tokenResponse.RefreshToken,
            tokenResponse.ExpiresIn);
    }

    // ── Flights ───────────────────────────────────────────────────────────────

    [Authorize(Roles = new[] { "Admin", "Staff" })]
    [GraphQLDescription("(Staff/Admin) Create a new flight.")]
    public async Task<FlightPayload> CreateFlight(
        CreateFlightInput input,
        [Service] IFlightService flightService,
        [Service] ITopicEventSender eventSender)
    {
        var flight = await flightService.CreateFlightAsync(
            input.FlightNumber, input.Airline,
            input.Origin, input.Destination,
            input.ScheduledDeparture, input.ScheduledArrival,
            input.Direction, input.GateId);

        await eventSender.SendAsync(nameof(Subscription.OnFlightCreated), flight);

        return new FlightPayload(flight);
    }

    [Authorize(Roles = new[] { "Admin", "Staff" })]
    [GraphQLDescription("(Staff/Admin) Update an existing flight's status, times, or gate.")]
    public async Task<FlightPayload> UpdateFlight(
        UpdateFlightInput input,
        [Service] IFlightService flightService,
        [Service] ITopicEventSender eventSender)
    {
        var flight = await flightService.UpdateFlightAsync(
            input.Id, input.Status, input.ActualDeparture,
            input.ActualArrival, input.DelayReason, input.GateId);

        await eventSender.SendAsync(
            $"{nameof(Subscription.OnFlightUpdated)}_{flight.Id}", flight);

        await eventSender.SendAsync(nameof(Subscription.OnAnyFlightUpdated), flight);

        return new FlightPayload(flight);
    }

    [Authorize]
    [GraphQLDescription("Follow a flight to receive real-time updates.")]
    public async Task<FlightFollowPayload> FollowFlight(
        FollowFlightInput input,
        [Service] IFlightService flightService,
        [Service] IUserSyncService userSync,
        [Service] IHttpContextAccessor httpContextAccessor)
    {
        var user = await userSync.SyncAsync(httpContextAccessor.HttpContext!.User);
        var follow = await flightService.FollowFlightAsync(user.Id, input.FlightId);
        return new FlightFollowPayload(follow);
    }

    [Authorize]
    [GraphQLDescription("Unfollow a flight.")]
    public async Task<UnfollowPayload> UnfollowFlight(
        UnfollowFlightInput input,
        [Service] IFlightService flightService,
        [Service] IHttpContextAccessor httpContextAccessor)
    {
        var userId = httpContextAccessor.HttpContext!.User.GetUserId();
        var success = await flightService.UnfollowFlightAsync(userId, input.FlightId);
        return new UnfollowPayload(success);
    }

    // ── Gates ─────────────────────────────────────────────────────────────────

    [Authorize(Roles = new[] { "Admin", "Staff" })]
    [GraphQLDescription("(Staff/Admin) Create a new gate.")]
    public async Task<GatePayload> CreateGate(
        CreateGateInput input,
        [Service] IGateService gateService)
    {
        var gate = await gateService.CreateGateAsync(input.GateNumber, input.Terminal);
        return new GatePayload(gate);
    }

    [Authorize(Roles = new[] { "Admin", "Staff" })]
    [GraphQLDescription("(Staff/Admin) Update a gate's details or availability.")]
    public async Task<GatePayload> UpdateGate(
        UpdateGateInput input,
        [Service] IGateService gateService)
    {
        var gate = await gateService.UpdateGateAsync(
            input.Id, input.GateNumber, input.Terminal, input.IsAvailable);
        return new GatePayload(gate);
    }

    [Authorize(Roles = new[] { "Admin" })]
    [GraphQLDescription("(Admin) Delete a gate. The gate must have no assigned flights.")]
    public async Task<DeleteGatePayload> DeleteGate(
        DeleteGateInput input,
        [Service] IGateService gateService)
    {
        var success = await gateService.DeleteGateAsync(input.Id);
        return new DeleteGatePayload(success);
    }

    [Authorize(Roles = new[] { "Admin", "Staff" })]
    [GraphQLDescription("(Staff/Admin) Assign a flight to a gate.")]
    public async Task<GatePayload> AssignGate(
        AssignGateInput input,
        [Service] IGateService gateService,
        [Service] IFlightService flightService,
        [Service] ITopicEventSender eventSender)
    {
        var gate = await gateService.AssignFlightToGateAsync(input.GateId, input.FlightId);
        var flight = await flightService.GetFlightByIdAsync(input.FlightId);
        await eventSender.SendAsync(nameof(Subscription.OnAnyFlightUpdated), flight);
        return new GatePayload(gate);
    }

    [Authorize(Roles = new[] { "Admin", "Staff" })]
    [GraphQLDescription("(Staff/Admin) Release a gate by removing the flight's gate assignment.")]
    public async Task<GatePayload> ReleaseGate(
        ReleaseGateInput input,
        [Service] IGateService gateService,
        [Service] IFlightService flightService,
        [Service] ITopicEventSender eventSender)
    {
        var gate = await gateService.ReleaseGateFromFlightAsync(input.FlightId);
        var flight = await flightService.GetFlightByIdAsync(input.FlightId);
        await eventSender.SendAsync(nameof(Subscription.OnAnyFlightUpdated), flight);
        return new GatePayload(gate);
    }
}
