# AirportSystem

A microservices airport management system. Staff manage flights and gates through a GraphQL API; passengers query schedules, book seats, follow flights for live updates, and ask an AI assistant for help. All services run behind a single gateway.

---

## Quickstart (Docker Compose)

### Prerequisites

- Docker + Docker Compose
- Git

### 1. Clone and configure

```bash
git clone <repo-url>
cd AirportSystem
```

Create a `.env` file in the project root:

```env
# MySQL (payment service)
MYSQL_ROOT_PASSWORD=yourpassword
MYSQL_DATABASE=airport_payment
MYSQL_EXTERNAL_PORT=3308

# Stripe — use test keys from dashboard.stripe.com
STRIPE_SECRET_KEY=sk_test_...
STRIPE_WEBHOOK_SECRET=whsec_...

# Google OAuth — leave empty to disable the Google login button
GOOGLE_CLIENT_ID=
GOOGLE_SECRET=

# URL the browser uses to reach the UI (used for Stripe redirect URLs)
UI_BASE_URL=http://localhost
```

### 2. Start

```bash
docker compose up -d --build
```

Keycloak imports the realm on first boot (~30–60 s). Everything else waits for its dependencies automatically.

### 3. Open the app

| URL | What it is |
|-----|-----------|
| `http://localhost` | Frontend UI |
| `http://localhost/graphql` | GraphQL playground (Banana Cake Pop) |
| `http://localhost:8080` | Keycloak admin console (admin / admin) |
| `http://localhost:15672` | RabbitMQ management (guest / guest) |

### 4. Seed test flights (optional)

```bash
python3 seed-flights.py
```

Creates 28 flights (departures and arrivals) relative to the current time.

### 5. First login

Self-register at `http://localhost/pages/login.html`, or use a pre-seeded account:

| Email | Password | Role |
|-------|----------|------|
| `admin@airport.local` | `Admin1234!` | Admin |
| `staff@airport.local` | `Staff1234!` | Staff |

---

## Architecture

```
Browser
  └─▶ nginx :80  ──▶  /graphql, /api/*, /assistant/*
                  └─▶  gateway :5000  ──▶  flight service
                                       ──▶  booking service
                                       ──▶  baggage service
                                       ──▶  payment service
                                       ──▶  assistant service
```

All inbound traffic hits nginx. The gateway validates JWTs and proxies to the correct backend; downstream services receive trusted `X-User-*` headers and do not re-validate tokens.

| Service | Tech | Notes |
|---------|------|-------|
| **UI** | nginx + static HTML/JS | Passenger and staff frontend |
| **Gateway** | .NET 8, YARP | JWT validation, routing |
| **Flight** | .NET 8, HotChocolate, PostgreSQL | Core domain: flights, gates, follows |
| **Booking** | .NET 8, REST, PostgreSQL | Seat bookings |
| **Baggage** | .NET 8, REST, PostgreSQL | Baggage tracking |
| **Payment** | Node.js, Stripe, MySQL | Stripe checkout sessions |
| **Notification** | .NET 8, RabbitMQ | Email dispatch |
| **Assistant** | Python, FastAPI, Ollama | AI flight queries |
| **Keycloak** | Keycloak 23 | Identity provider, JWT issuer |
| **RabbitMQ** | RabbitMQ 3 | Async messaging |
| **PostgreSQL** | PostgreSQL 16 | Shared by flight/booking/keycloak |
| **MySQL** | MySQL 8 | Payment service only |
| **Ollama** | ollama/ollama | Local LLM runtime |

See [`k8s/architecture.md`](k8s/architecture.md) for a detailed diagram.

---

## Service READMEs

- [Flight service](flight/README.md) — GraphQL API reference, migrations, running locally
- [Gateway](gateway/README.md) — routing table, JWT setup, adding new services
- [Assistant service](AssistantService/README.md) — chat API, Ollama model, local dev
- [Kubernetes](k8s/README.md) — minikube deploy, access URLs, rebuilding

---

## Auth flow

1. User logs in at `/pages/login.html` via Keycloak PKCE flow → receives a JWT in `localStorage`
2. Every API request includes `Authorization: Bearer <token>`
3. The gateway validates the token signature against Keycloak's JWKS and extracts `realm_access.roles`
4. Trusted `X-User-Id`, `X-User-Email`, `X-User-Name`, `X-User-Roles` headers are forwarded to downstream services
5. Downstream services trust those headers without re-validating the JWT

Roles: `Admin` (full access), `Staff` (flight/gate operations), `Passenger` (default, booking/follow).

---

## Rebuilding a single service

```bash
docker compose up -d --build <service-name>
# e.g.
docker compose up -d --build flight
docker compose up -d --build gateway
```
