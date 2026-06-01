# AirportSystem

A microservices-based airport management system. Staff manage flights, gates, and bookings through a GraphQL API; passengers can query schedules, book seats, follow flights for live updates, and ask an AI assistant for help. All services run behind a single gateway and communicate internally via RabbitMQ and direct HTTP.

---

## Running the system

### Docker Compose

```bash
docker compose up --build
```

On first run Keycloak takes ~30 seconds to import the realm. Everything else waits for its dependencies automatically.

| Endpoint | URL |
|---|---|
| API gateway | `http://localhost:5000` |
| GraphQL playground (Banana Cake Pop) | `http://localhost:5000/graphql` |
| AI assistant | `http://localhost:5000/assistant/chat` |
| Keycloak admin | `http://localhost:8080` (admin / admin) |
| RabbitMQ management | `http://localhost:15672` (guest / guest) |
| Baggage API | `http://localhost:5002` |
| Payment service | `http://localhost:3000` |

The first time the assistant starts it pulls the `qwen2.5:3b` model from Ollama (~2 GB). This takes a few minutes but is cached in a Docker volume — subsequent starts are instant.

### Kubernetes (minikube)

```bash
cd k8s
./deploy.sh
```

The script builds all images into minikube's Docker daemon, applies manifests, and prints access URLs. Requires minikube to be running.

| Endpoint | URL |
|---|---|
| API gateway | `http://<minikube-ip>:30500` |
| Keycloak admin | `http://<minikube-ip>:30880` (admin / admin) |
| RabbitMQ management | `http://<minikube-ip>:30672` (guest / guest) |
| Baggage API | `http://<minikube-ip>:30501` |

---

## Services

### Gateway (`/gateway`)
YARP reverse proxy (.NET 8). The only public entry point. Validates JWTs against Keycloak and forwards requests to the appropriate backend service. Routes `/assistant/*` to the assistant service and everything else to the flight service.

### Flight service (`/flight`)
The core domain service. HotChocolate GraphQL API (.NET 8, EF Core, PostgreSQL). Handles flights, gates, bookings, and flight follows. Exposes queries, mutations, and WebSocket subscriptions for real-time flight updates. Publishes flight-change events to RabbitMQ to fan out notifications to followers.

### Booking service (`/BookingService`)
REST API (.NET, PostgreSQL) for seat bookings. Consumes `PaymentStatusMessage` from the `booking_queue` — when a payment succeeds the booking is marked `Confirmed`; when it fails it is marked `Cancelled`.

### Notification service (`/NotificationService`)
Background worker (.NET). Consumes `Notification` messages from RabbitMQ and sends emails. Kept generic — it does not know about flights, only about the message it receives. The flight service formats and addresses the messages before publishing.

### Baggage API (`/BaggageAPI`)
REST API (.NET) for baggage tracking. Runs in its own namespace in k8s and has its own PostgreSQL instance, mirroring an isolated deployment model.

### Payment service (`/PaymentService`)
Node.js service that handles payment processing via Stripe. Listens on RabbitMQ for payment events from the booking flow and publishes the outcome back to `booking_queue`.

### Assistant service (`/AssistantService`)
Python/FastAPI service. On each request it fetches the current flight list from the flight service, injects it into a system prompt, and queries a locally-running Ollama instance (`qwen2.5:3b`) to answer passenger questions in natural language. No external API calls — fully offline.

### Keycloak
Identity provider. Issues JWTs for login/registration. The realm is auto-imported from `keycloak/airport-realm.json` on first start.

Seed accounts:

| Email | Password | Role |
|---|---|---|
| `admin@airport.local` | `Admin1234!` | Admin |
| `staff@airport.local` | `Staff1234!` | Staff |

Passengers self-register via the `register` GraphQL mutation.

---

## Auth flow

Clients authenticate through the flight service GraphQL (`login` / `register` mutations), which exchange credentials with Keycloak and return a JWT. All subsequent requests carry that JWT in the `Authorization: Bearer` header. The gateway validates the token before forwarding; services receive trusted `X-User-*` headers and do not re-validate.
