using AirportSystem.Flights.Models;
using AirportSystem.Flights.Services.Auth;

namespace AirportSystem.Flights.GraphQL.Payloads;

// ── Auth ──────────────────────────────────────────────────────────────────────

/// <summary>
/// Returned by login/register mutations.
/// AccessToken is the Keycloak JWT — pass it as "Authorization: Bearer {token}"
/// on all subsequent requests (and as ?access_token= for WebSocket subscriptions).
/// </summary>
public record AuthPayload(
    string AccessToken,
    string? RefreshToken,
    int ExpiresIn,
    User? User = null          // null on login; populated after first sync
);

// ── Flights ───────────────────────────────────────────────────────────────────

public record FlightPayload(Flight Flight);
public record BookingPayload(Booking Booking);
public record FlightFollowPayload(FlightFollow FlightFollow);
public record UnbookPayload(bool Success);
public record UnfollowPayload(bool Success);

// ── Gates ─────────────────────────────────────────────────────────────────────

public record GatePayload(Gate Gate);
public record DeleteGatePayload(bool Success);
