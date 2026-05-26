# TODO: THIS IS ALL AI GARBAGE REDO THIS
it is also incorrect

# ✈️ AirportSystem — GraphQL API

A production-ready **C# / .NET 8** GraphQL API for managing airport flight operations, built with **Hot Chocolate 13**, **Entity Framework Core 8**, **PostgreSQL**, and **Keycloak 23** for identity and access management.

---

## 📐 Architecture

```
AirportSystem/
├── keycloak/
│   └── airport-realm.json          # Auto-imported realm (roles, clients, seed users)
├── src/
│   ├── AirportSystem.API/
│   │   ├── Data/                   # AppDbContext + EF migrations
│   │   ├── Extensions/             # ServiceExtensions, ClaimsPrincipalExtensions
│   │   ├── GraphQL/                # Query, Mutation, Subscription, ErrorFilter
│   │   │   ├── Inputs/             # Input record types (Auth, Flights, Gates)
│   │   │   ├── Payloads/           # Mutation return types
│   │   │   └── Types/              # Hot Chocolate type configurations
│   │   ├── Models/                 # Domain entities + enums
│   │   ├── Migrations/             # EF Core migrations
│   │   └── Services/
│   │       ├── Auth/               # KeycloakService, KeycloakClaimsTransformer, UserSyncService
│   │       ├── Flights/            # FlightService
│   │       └── Gates/              # GateService
│   └── AirportSystem.Tests/
│       ├── Helpers/                # DbContextFactory, TestDataBuilder
│       └── Services/               # Unit tests for all services
├── docker-compose.yml
└── AirportSystem.sln
```

---

## 🔐 Authentication Architecture

```
Browser / Client
     │
     │  1. POST /graphql  { login(input: {...}) }
     ▼
 AirportSystem API  ──────────────────────►  Keycloak
     │               ROPC grant / Admin API       │
     │  ◄──────────────────────────────────  JWT (access + refresh token)
     │
     │  2. All subsequent requests:
     │     Authorization: Bearer <access_token>
     ▼
 AirportSystem API validates JWT against Keycloak's JWKS endpoint
     │
     │  3. KeycloakClaimsTransformer unpacks realm_access.roles → ClaimTypes.Role
     │  4. UserSyncService upserts a local User row from the JWT sub claim
     ▼
 GraphQL resolver executes with full identity context
```

### Two Keycloak Clients

| Client | Type | Used for |
|---|---|---|
| `airport-api` | Confidential (service account) | Admin REST calls — creating users, assigning roles |
| `airport-frontend` | Public (PKCE) | Browser/playground token requests |

### Role-based Authorization

| Role | Permissions |
|---|---|
| `Passenger` | View flights & gates, book/unbook, follow/unfollow flights |
| `Staff` | All Passenger + create/update flights, create/update gates, assign/release gates |
| `Admin` | All Staff + delete gates, view all users |

### Seed Accounts (created on first Keycloak startup)

| Email | Password | Role |
|---|---|---|
| `admin@airport.local` | `Admin1234!` | Admin |
| `staff@airport.local` | `Staff1234!` | Staff |

---

## 🚀 Quick Start

### Option A — Docker Compose (recommended)

```bash
docker-compose up --build
```

| Service | URL |
|---|---|
| GraphQL API + Banana Cake Pop | `http://localhost:5000/graphql` |
| Keycloak Admin Console | `http://localhost:8080` (admin / admin) |
| PostgreSQL | `localhost:5432` |

> **First run:** Keycloak takes ~30 seconds to start and import the realm. The API container will wait for it automatically.

### Option B — Local .NET

```bash
# 1. Start infrastructure only
docker-compose up postgres keycloak

# 2. Run the API locally
cd src/AirportSystem.API
dotnet restore
dotnet run
```

### Run Tests

```bash
cd src/AirportSystem.Tests
dotnet restore
dotnet test --logger "console;verbosity=normal"
```

Tests use **in-memory EF Core** and **mocked HttpClient** — no running services required.

---

## ⚙️ Configuration

All Keycloak settings can be overridden via environment variables using the `__` separator:

| Key | Description | Default |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string | localhost / postgres |
| `Keycloak__BaseUrl` | Keycloak base URL | `http://localhost:8080` |
| `Keycloak__Realm` | Realm name | `airport-system` |
| `Keycloak__ClientId` | Confidential client id (service account) | `airport-api` |
| `Keycloak__ClientSecret` | Confidential client secret | `airport-api-secret` |
| `Keycloak__FrontendClientId` | Public client id (user login) | `airport-frontend` |

> **Production note:** Change `ClientSecret`, use a persistent Keycloak DB (not `dev-mem`), and put Keycloak behind TLS.

---

## 📡 GraphQL Operations

Open Banana Cake Pop at `http://localhost:5000/graphql`.
After login, set the **Authorization** header to `Bearer <access_token>`.

### Authentication

```graphql
# Register a new Passenger account
mutation Register {
  register(input: {
    username: "alice"
    email:    "alice@example.com"
    password: "Password123!"
    role:     "Passenger"
  }) {
    accessToken
    refreshToken
    expiresIn
  }
}

# Log in (works for all roles)
mutation Login {
  login(input: {
    email:    "admin@airport.local"
    password: "Admin1234!"
  }) {
    accessToken
    refreshToken
    expiresIn
  }
}
```

### My Profile

```graphql
# Get the currently authenticated user (auto-provisioned from JWT)
query Me {
  me {
    id username email role createdAt lastSeenAt
  }
}
```

### Flights

```graphql
# List all scheduled departures
query Departures {
  flights(direction: DEPARTURE, status: SCHEDULED) {
    id flightNumber airline origin destination
    scheduledDeparture status
    gate { gateNumber terminal }
  }
}

# Create a flight  (Staff / Admin)
mutation CreateFlight {
  createFlight(input: {
    flightNumber:      "SK101"
    airline:           "SAS"
    origin:            "CPH"
    destination:       "LHR"
    scheduledDeparture: "2025-06-01T08:00:00Z"
    scheduledArrival:   "2025-06-01T10:00:00Z"
    direction:         DEPARTURE
  }) {
    flight { id flightNumber status }
  }
}

# Update flight status  (Staff / Admin)
mutation UpdateFlight {
  updateFlight(input: {
    id:     "<flight-uuid>"
    status: BOARDING
  }) {
    flight { id flightNumber status updatedAt }
  }
}

# Mark a flight as delayed  (Staff / Admin)
mutation DelayFlight {
  updateFlight(input: {
    id:          "<flight-uuid>"
    status:      DELAYED
    delayReason: "Late inbound aircraft"
  }) {
    flight { id status delayReason }
  }
}

# Book a flight
mutation Book {
  bookFlight(input: { flightId: "<flight-uuid>", seatNumber: "14A" }) {
    booking { id seatNumber bookedAt flight { flightNumber } }
  }
}

# Follow a flight for real-time updates
mutation Follow {
  followFlight(input: { flightId: "<flight-uuid>" }) {
    flightFollow { id followedAt flight { flightNumber } }
  }
}

# My bookings
query MyBookings {
  myBookedFlights {
    id flightNumber status scheduledDeparture
    gate { gateNumber terminal }
  }
}

# My followed flights
query MyFollowed {
  myFollowedFlights {
    id flightNumber status delayReason
  }
}
```

### Gates

```graphql
# All available gates
query AvailableGates {
  gates(availableOnly: true) {
    id gateNumber terminal isAvailable
    flights { flightNumber status }
  }
}

# Create gate  (Staff / Admin)
mutation CreateGate {
  createGate(input: { gateNumber: "A1", terminal: "A" }) {
    gate { id gateNumber terminal isAvailable }
  }
}

# Assign flight to gate  (Staff / Admin)
mutation AssignGate {
  assignGate(input: { gateId: "<gate-uuid>", flightId: "<flight-uuid>" }) {
    gate { gateNumber isAvailable flights { flightNumber } }
  }
}

# Release gate  (Staff / Admin)
mutation ReleaseGate {
  releaseGate(input: { flightId: "<flight-uuid>" }) {
    gate { gateNumber isAvailable }
  }
}
```

### Real-time Subscriptions (WebSocket)

Connect via `ws://localhost:5000/graphql?access_token=<your-jwt>`.

```graphql
# All new flights as they are created
subscription NewFlights {
  onFlightCreated {
    id flightNumber airline origin destination scheduledDeparture
  }
}

# Live updates for a specific flight (status, gate, delay)
subscription TrackFlight {
  onFlightUpdated(flightId: "<flight-uuid>") {
    id flightNumber status delayReason
    gate { gateNumber terminal }
  }
}
```

---

## 🗄️ Database Migrations

Migrations run automatically on startup via `MigrateAsync()`.

To create a new migration after a model change:

```bash
cd src/AirportSystem.API
dotnet ef migrations add <MigrationName> --output-dir Migrations
```

---

## 🧪 Test Coverage

| Test class | Scenarios |
|---|---|
| `KeycloakServiceTests` | Login success/failure, register success/duplicate, claims transformer role mapping, missing realm_access |
| `UserSyncServiceTests` | First-time provision, id matches Keycloak sub, role detection, profile updates, LastSeenAt, guard clauses |
| `FlightServiceTests` | Create (valid, bad dates, gate), update, book/unbook, follow/unfollow, filtered queries |
| `GateServiceTests` | Create, update, delete (blocked/allowed), assign, release, availability filter |

