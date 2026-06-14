# Flight Service

Core domain service for the Airport System. Exposes a HotChocolate GraphQL API (.NET 8) backed by PostgreSQL. Manages flights, gates, and flight-follows, and publishes real-time events to downstream services via RabbitMQ and WebSocket subscriptions.

---

## Running locally

```bash
cd flight
DOTNET_ROLL_FORWARD=Major dotnet run --project src/AirportSystem.Flights
```

GraphQL playground (Banana Cake Pop) is available at `http://localhost:5000/graphql` when running standalone.

In Docker Compose the service is reachable via the gateway at `http://localhost/graphql`.

### Environment variables

| Variable | Default | Description |
|----------|---------|-------------|
| `ConnectionStrings__DefaultConnection` | — | PostgreSQL connection string |
| `RabbitMQ__Host` | `rabbitmq` | RabbitMQ hostname |
| `RabbitMQ__Port` | `5672` | RabbitMQ port |
| `RabbitMQ__Username` | `guest` | RabbitMQ username |
| `RabbitMQ__Password` | `guest` | RabbitMQ password |
| `ASPNETCORE_ENVIRONMENT` | `Production` | Set to `Development` to enable the dev data seeder |

---

## GraphQL API

The full schema is browsable in Banana Cake Pop. Below is a summary of every operation.

### Queries

```graphql
# All flights. Supports optional filtering and built-in HotChocolate filtering/sorting.
flights(direction: FlightDirection, status: FlightStatus): [Flight!]!

# Single flight by ID.
flight(id: UUID!): Flight!

# Flights the authenticated user is following.  Requires auth.
myFollowedFlights: [Flight!]!

# All gates. Pass availableOnly: true to filter.
gates(availableOnly: Boolean): [Gate!]!

# Single gate by ID.
gate(id: UUID!): Gate!

# The authenticated user's profile.  Requires auth.
me: User!
```

### Mutations

**Flights**

```graphql
# Create a flight.  No auth required.
createFlight(input: CreateFlightInput!): FlightPayload!

# Update status, actual time, delay reason, or gate.  Requires Staff or Admin.
updateFlight(input: UpdateFlightInput!): FlightPayload!

# Follow a flight to receive email notifications on updates.  Requires auth.
followFlight(input: FollowFlightInput!): FlightFollowPayload!

# Stop following a flight.  Requires auth.
unfollowFlight(input: UnfollowFlightInput!): UnfollowPayload!
```

**Gates**

```graphql
# Create a gate.  Requires Staff or Admin.
createGate(input: CreateGateInput!): GatePayload!

# Update gate number, terminal, or availability.  Requires Staff or Admin.
updateGate(input: UpdateGateInput!): GatePayload!

# Delete a gate.  Gate must have no flights assigned.  Requires Admin.
deleteGate(input: DeleteGateInput!): DeleteGatePayload!

# Assign a flight to a gate.  Requires Staff or Admin.
assignGate(input: AssignGateInput!): GatePayload!

# Remove a flight's gate assignment.  Requires Staff or Admin.
releaseGate(input: ReleaseGateInput!): GatePayload!
```

### Subscriptions

Subscriptions use the `graphql-transport-ws` WebSocket sub-protocol.

```graphql
# Fires when any new flight is created.
onFlightCreated: Flight!

# Fires when a specific flight is updated.  Requires auth.
onFlightUpdated(flightId: UUID!): Flight!

# Fires when any flight is updated.  Used by the public departure/arrival board.
onAnyFlightUpdated: Flight!
```

### Key types

```graphql
type Flight {
  id: UUID!
  flightNumber: String!
  airline: String!
  origin: String!
  destination: String!
  direction: FlightDirection!   # DEPARTURE | ARRIVAL
  status: FlightStatus!         # SCHEDULED | BOARDING | DEPARTED | ARRIVED | DELAYED | CANCELLED
  scheduledTime: DateTime!
  actualTime: DateTime
  delayReason: String
  gate: Gate
}

type Gate {
  id: UUID!
  gateNumber: String!
  terminal: String!
  isAvailable: Boolean!
}
```

---

## Roles

| Role | Allowed operations |
|------|--------------------|
| Anyone | `flights`, `flight`, `gates`, `gate`, `createFlight`, `onFlightCreated`, `onAnyFlightUpdated` |
| Authenticated | `myFollowedFlights`, `me`, `followFlight`, `unfollowFlight`, `onFlightUpdated` |
| Staff | all above + `updateFlight`, `createGate`, `updateGate`, `assignGate`, `releaseGate` |
| Admin | all above + `deleteGate` |

---

## Database migrations

Migrations are applied automatically at startup via `db.Database.Migrate()`.

To add a new migration (requires .NET SDK 8+):

```bash
cd flight
DOTNET_ROLL_FORWARD=Major dotnet ef migrations add <MigrationName> \
  --project src/AirportSystem.Flights \
  --startup-project src/AirportSystem.Flights
```

If only .NET 10 is installed, prefix every `dotnet` command with `DOTNET_ROLL_FORWARD=Major`.

---

## Tests

```bash
cd flight
DOTNET_ROLL_FORWARD=Major dotnet test
```

Tests use an in-memory SQLite database. No external services are required.

---

## Dev data seeder

When `ASPNETCORE_ENVIRONMENT=Development` and the database is empty, `DevDataSeeder` inserts a small set of flights and gates automatically. To reseed, truncate the `Flights` and `Gates` tables and restart the service.

For a larger realistic dataset use the project-root seed script:

```bash
python3 ../seed-flights.py http://localhost:5000/graphql
```
