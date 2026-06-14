using AirportSystem.Flights.Models;

namespace AirportSystem.Flights.GraphQL.Payloads;

// ── Flights ───────────────────────────────────────────────────────────────────

public record FlightPayload(Flight Flight);
public record FlightFollowPayload(FlightFollow FlightFollow);
public record UnfollowPayload(bool Success);

// ── Gates ─────────────────────────────────────────────────────────────────────

public record GatePayload(Gate Gate);
public record DeleteGatePayload(bool Success);
