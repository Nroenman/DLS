# Airport Management System
## Final Project Report
### Development of Large Systems

---

**Title:** Airport Management System — A Distributed Microservices Architecture

**Group Members:** [INSERT: Full names of all students]

**Date of Delivery:** [INSERT: Delivery date]

**Institution:** KEA — Copenhagen School of Design and Technology

---

## List of Figures

- Figure 1: Overall system architecture diagram [PLACEHOLDER: insert diagram]
- Figure 2: Docker Compose development environment diagram [PLACEHOLDER: insert diagram]
- Figure 3: Kubernetes deployment architecture diagram [PLACEHOLDER: insert diagram]
- Figure 4: CI/CD pipeline diagram [PLACEHOLDER: insert diagram when pipeline is built]
- Figure 5: Booking Saga flow diagram [PLACEHOLDER: insert sequence diagram]
- Figure 6: CQRS pattern in BookingService [PLACEHOLDER: insert diagram]
- Figure 7: Cloud deployment plan diagram [PLACEHOLDER: insert diagram]

---

## List of Appendices

- Appendix A: OpenAPI / Swagger specifications for REST APIs
- Appendix B: GraphQL schema for FlightService
- Appendix C: AsyncAPI documentation for RabbitMQ message contracts
- Appendix D: Keycloak realm configuration (`airport-realm.json`)
- Appendix E: k6 stress test results

---

## Table of Contents

1. Introduction
   - 1.1 Problem Description
   - 1.2 Functional and Non-Functional Requirements
   - 1.3 Technology Stack Choices
2. System Architecture
   - 2.1 Microservices Architecture Overview
   - 2.2 Microservices Description
   - 2.3 Communication Between Microservices
   - 2.4 Patterns and Techniques
3. Environments Description
   - 3.1 Development Environment (Docker Compose)
   - 3.2 Local Kubernetes Deployment
   - 3.3 Cloud Deployment Plan
4. Testing
5. Project Management and Team Collaboration
6. Discussion
7. Reflection
8. Conclusion
9. References
10. Appendix

---

## 1. Introduction

### 1.1 Problem Description

Airports handle thousands of passengers daily across departing and arriving flights, each of which involves coordination between gate assignment, baggage handling, booking management, payment processing, and passenger communication. Legacy monolithic systems struggle to scale individual bottlenecks — for instance, a surge in booking activity should not degrade the flight information display, and a payment processing delay should not block other unrelated operations.

This project addresses that problem by designing and implementing an airport management system as a distributed, microservices-based application. The system allows airport staff to manage flights, gates, and passenger bookings, while passengers can search for flights, make bookings, track their baggage, and get real-time flight status updates. An AI assistant provides a natural-language interface for passenger queries about departures, arrivals, and gate information.

The system was built to demonstrate the properties expected of large-scale distributed systems: horizontal scalability, independent deployability of each domain service, asynchronous communication between services, and fault isolation so that the failure of one service does not cascade into the rest.

### 1.2 Functional and Non-Functional Requirements

**Functional Requirements**

| ID | Requirement |
|----|-------------|
| FR-01 | Staff can create, update, and cancel flights with gate assignments |
| FR-02 | Passengers can view flight schedules, filtered by direction and status |
| FR-03 | Passengers can follow specific flights and receive email notifications on status changes |
| FR-04 | Passengers can book a flight (one-way or return), selecting seat class and providing passenger details |
| FR-05 | Payment is processed as part of the booking flow; a booking is confirmed only after successful payment |
| FR-06 | Passengers can check in baggage associated with a booking and track its location through defined status stages |
| FR-07 | Passengers can query a natural-language AI assistant for flight information |
| FR-08 | Real-time flight status updates are pushed to connected clients via GraphQL subscriptions |
| FR-09 | Users authenticate via Keycloak; third-party identity providers (e.g. Google) can be configured as identity brokers |
| FR-10 | Role-based access control restricts operations: Admin and Staff manage flights; Passengers manage their own bookings |

**Non-Functional Requirements**

| ID | Requirement |
|----|-------------|
| NFR-01 | Each backend microservice must be independently deployable in a container |
| NFR-02 | Services communicate asynchronously via RabbitMQ message queues to decouple producers from consumers |
| NFR-03 | The system must support horizontal scaling; the Booking Service scales via HPA and the Notification Service scales to zero via KEDA |
| NFR-04 | Each service owns its own data and does not access another service's database directly |
| NFR-05 | Authentication uses JWT tokens validated at the gateway; services receive identity via trusted forwarded headers |
| NFR-06 | The development environment is fully reproducible via `docker-compose up` |
| NFR-07 | The production-like environment runs on a local Kubernetes cluster (minikube) |
| NFR-08 | The AI service runs locally via Ollama to avoid dependency on paid external APIs |
| NFR-09 | All services expose structured logging; metrics and logs are collected by Prometheus, Grafana, and Loki |
| NFR-10 | API versioning is applied to all REST APIs |

### 1.3 Technology Stack Choices

**Backend services (.NET 8 / .NET 10 — C#)**

The majority of backend services use the .NET platform. .NET was chosen because it offers strong typing, built-in dependency injection, mature async/await support, and well-supported libraries for Entity Framework Core (database access), HotChocolate (GraphQL), and YARP (reverse proxy). The performance characteristics of .NET 8+ in containerized workloads are comparable to Go and considerably better than interpreted languages under load.

**Payment Service (Node.js + Express)**

The Payment Service was implemented in Node.js to demonstrate polyglot capability and because the Stripe SDK ecosystem is well-established in JavaScript. Node.js handles I/O-bound workloads like payment API calls efficiently with its event loop model.

**AI Assistant Service (Python + FastAPI)**

Python was chosen for the Assistant Service because of the mature ecosystem for AI/ML tooling. FastAPI provides async HTTP with minimal boilerplate, and `httpx` handles async HTTP calls to Ollama and the Flight Service's GraphQL API.

**Databases (PostgreSQL)**

PostgreSQL was chosen as the primary relational database across services. It supports advanced query features, has excellent EF Core support, handles JSON columns when needed, and is well-supported in Kubernetes through official images. Separate PostgreSQL instances are used to enforce data ownership — the Baggage Service runs its own isolated PostgreSQL instance, while other services share a separate instance scoped to their schemas.

**Message Broker (RabbitMQ)**

RabbitMQ was chosen for asynchronous messaging because it is widely used, supports AMQP, provides durable queues with manual acknowledgment, and has a management UI useful during development. The workload does not require the event log semantics of Kafka, making RabbitMQ a more appropriate fit.

**Identity and Access Management (Keycloak)**

Keycloak was chosen as the identity provider because it supports OIDC, OAuth 2.0, JWT issuance, and third-party identity brokering out of the box. Configuring Keycloak as a broker for Google login or other providers requires only realm-level configuration without application code changes.

**Container Orchestration (Kubernetes via minikube)**

Kubernetes is the industry standard for container orchestration. Minikube allows a production-like Kubernetes environment to be run locally. KEDA is used alongside the Kubernetes HPA to provide event-driven autoscaling tied to RabbitMQ queue depth.

**AI Model (Ollama + qwen2.5:3b)**

Ollama provides a self-hosted LLM runtime that can run models locally in a Docker container. The `qwen2.5:3b` model (3 billion parameters) is small enough to run on a development machine without a GPU while still being capable of answering contextual questions from a structured prompt. This approach avoids any dependency on paid external APIs.

---

## 2. System Architecture

### 2.1 Microservices Architecture Overview

The system follows a microservices architecture with six backend services, an API gateway, an identity provider, and supporting infrastructure. Each service encapsulates a distinct business domain and communicates with other services either synchronously via REST or GraphQL (frontend-facing), or asynchronously via RabbitMQ (inter-service).

The API Gateway (YARP) is the single entry point for all client traffic. It validates JWT tokens issued by Keycloak, extracts identity claims, and forwards requests to the appropriate backend service with trusted identity headers. This pattern means backend services do not need to implement JWT validation individually.

[PLACEHOLDER: Figure 1 — Insert overall system architecture diagram showing all services, databases, message queues, the gateway, Keycloak, and the AI assistant with their interconnections]

The six backend microservices are:

1. **Flight Service** — manages flights, gates, and flight-following subscriptions
2. **Booking Service** — handles seat reservations and coordinates with payment
3. **Payment Service** — processes payments via Stripe and communicates results asynchronously
4. **Notification Service** — listens for flight update events and sends email notifications
5. **Baggage API** — tracks baggage items through the handling lifecycle
6. **Assistant Service** — answers natural-language passenger queries using a local LLM

### 2.2 Microservices Description

---

#### 2.2.1 Flight Service

**Purpose and responsibilities**

The Flight Service is the central domain service for all flight-related operations. It manages the lifecycle of flights from creation through to completion or cancellation, and tracks which passengers are following which flights. It was isolated into its own service because flight data is read by the AI assistant, triggers notifications to passengers, and would otherwise create a tight coupling bottleneck if folded into the booking domain.

**Exposed APIs**

The Flight Service exposes a GraphQL API using the HotChocolate library on port 8080. GraphQL was chosen over REST here because the frontend has variable field requirements (a flight board needs different fields than a flight detail page) and because GraphQL subscriptions over WebSocket provide an efficient mechanism for real-time flight status updates without polling.

The schema exposes:
- **Queries:** `flights` (with optional `direction` and `status` filters), `flight(id)`, `gates`, `gate(id)`, `followedFlights`
- **Mutations:** `createFlight`, `updateFlight`, `createGate`, `followFlight`, `unfollowFlight`
- **Subscriptions:** `onFlightUpdated` — pushes real-time updates to subscribed clients via WebSocket when any flight is modified

Role-based access is enforced at the resolver level using `@Authorize` directives. Mutations that modify flights require the `Admin` or `Staff` role; read queries are open to authenticated passengers.

**Consumed and produced events**

The Flight Service publishes a `NotificationMessage` to the `Notification` queue on RabbitMQ whenever a flight is updated. The message contains an email address, subject, and body, generated by fanning out across all followers of the modified flight.

The Flight Service does not consume events from other services.

**Data storage and ownership**

The Flight Service owns a PostgreSQL database containing `Flights`, `Gates`, `Users` (a local user projection, populated via Keycloak JWT claims on login), and `FlightFollows`. EF Core is used for database access with code-first migrations. User records are synced from the Keycloak JWT payload on each authenticated request rather than being fetched from Keycloak directly, reducing coupling to the identity provider.

**Key business logic**

When a flight is updated via the `updateFlight` mutation, the service persists the change, triggers a GraphQL subscription event (pushing the update to connected clients), and publishes a notification message to RabbitMQ for each follower's email address. This fan-out is done synchronously within the mutation handler, making the mutation the authoritative trigger for downstream notification.

Flights are never physically deleted. A cancelled flight transitions to `FlightStatus.Cancelled` and remains in the database. This acts as a tombstone record: any downstream consumer can distinguish between a flight that never existed and one that was cancelled.

**Scalability and failure considerations**

Under high read load, the GraphQL query layer can be scaled horizontally. The subscription infrastructure relies on an in-memory topic channel (HotChocolate's built-in topic), which means subscription events are not shared across multiple Flight Service replicas. In a production deployment this would be replaced with a Redis-backed subscription provider. Under mutation-heavy load (bulk flight updates), the notification fan-out becomes a bottleneck if a flight has many followers; this could be decoupled by instead publishing a single `FlightUpdated` event and having the Notification Service perform the fan-out.

---

#### 2.2.2 Booking Service

**Purpose and responsibilities**

The Booking Service handles seat reservation requests. A passenger submits a booking with flight ID(s), seat class, passenger details, and contact information. The service coordinates with the Payment Service asynchronously to confirm whether the payment succeeded before finalising the booking status. It was isolated because the booking lifecycle, pricing rules, and payment saga are distinct from flight data management and from baggage tracking.

**Exposed APIs**

The Booking Service exposes a REST API on port 5001. REST was chosen here because booking operations map naturally to HTTP verbs on resource-oriented endpoints (`POST /bookings`, `GET /bookings/{id}`, `GET /bookings/user`), and the request/response pattern matches the synchronous nature of form submissions from the frontend.

[PLACEHOLDER: reference Swagger/OpenAPI specification in Appendix A]

**Consumed and produced events**

When a booking is created, the Booking Service publishes a `PaymentMessage` to the `payment_queue` containing the booking ID, flight number, seat class, total price, and contact email. This starts the payment saga.

The Booking Service also runs a background consumer (`BookingEventConsumer`) on the `booking_queue`. When a `PaymentStatusMessage` arrives, it updates the booking's status to `Confirmed` or `Cancelled` depending on the `PaymentSucceeded` flag.

**Data storage and ownership**

The Booking Service owns a PostgreSQL database containing `Bookings` and `Passengers`. Database access is split into separate read and write repositories (`BookingReadRepository` and `BookingWriteRepository`), following the CQRS pattern. Migrations are managed via EF Core.

**Key business logic**

Booking status transitions follow a defined lifecycle: `Pending` on creation, `AwaitingPayment` once the payment message is published, `Confirmed` on successful payment, or `Cancelled` on failure. The service validates that required fields are present and that the referenced flight exists before creating the booking record.

**Scalability and failure considerations**

The Booking Service is configured with a Kubernetes HPA that scales between 1 and 5 replicas based on CPU utilisation (70% threshold) and memory utilisation (80% threshold). If the Payment Service is temporarily unavailable, the `payment_queue` retains messages until the Payment Service recovers, and the booking remains in `AwaitingPayment` state. There is currently no timeout or dead-letter queue for bookings stuck in `AwaitingPayment`; adding one would improve resilience.

---

#### 2.2.3 Notification Service

**Purpose and responsibilities**

The Notification Service is a stateless background worker that consumes notification messages from RabbitMQ and sends emails to passengers. It has no REST or GraphQL API, no database, and no outbound events. It was isolated because email delivery is an infrastructure concern that should not slow down or couple to the business logic of flight management or booking.

**Consumed and produced events**

The service consumes `NotificationMessage` records from the `Notification` queue. Each message contains a recipient email address, sender name, subject, and body. The service sends the email via SMTP (using MailKit) and acknowledges the message.

If email delivery fails, the message is negatively acknowledged and dropped. A production improvement would be to route failed messages to a dead-letter queue for retry or manual inspection.

**Data storage and ownership**

The Notification Service owns no persistent data. All state needed for an individual notification is contained in the message payload.

**Scalability and failure considerations**

In Kubernetes, the Notification Service is managed by a KEDA `ScaledObject` configured against the `notification-queue` queue length. It scales to zero replicas when the queue is empty and up to five replicas when queue depth reaches five messages per replica, with a 60-second cooldown to avoid thrashing on bursty traffic. This serverless-style scaling makes the Notification Service effectively a serverless function running on the cluster without requiring a managed Functions-as-a-Service platform.

---

#### 2.2.4 Payment Service

**Purpose and responsibilities**

The Payment Service processes payments using the Stripe API. It was isolated as a separate service because payment processing involves third-party integration with distinct security concerns, and because the billing domain should not share a deployment boundary with booking logic.

**Exposed APIs**

The Payment Service exposes a minimal internal REST API on port 3000 and participates in asynchronous communication via RabbitMQ. The REST endpoints are not exposed through the gateway and are used for internal health checks and webhook handling.

**Consumed and produced events**

The Payment Service runs a RabbitMQ consumer on the `payment_queue`. When a `PaymentMessage` is received, it calls the Stripe API to initiate a charge. It then publishes a `PaymentStatusMessage` to the `booking_queue` with the booking ID and a boolean indicating success or failure.

**Data storage and ownership**

The Payment Service maintains no persistent state itself. The Stripe dashboard and webhook system provide the authoritative record of payment transactions. This is an intentional trade-off: the service is stateless and easy to replace, but it relies on Stripe's availability for payment confirmation.

**Scalability and failure considerations**

Under high booking volume the `payment_queue` depth will grow if the Payment Service cannot process messages fast enough. The service can be scaled horizontally by increasing the replica count; the RabbitMQ queue ensures work is distributed across consumers without duplication.

---

#### 2.2.5 Baggage API

**Purpose and responsibilities**

The Baggage API manages the physical tracking of baggage items from check-in through loading, transit, and claim. It was isolated into its own namespace and database to demonstrate strong data ownership boundaries and to allow the baggage domain to evolve independently from the flight and booking domains.

**Exposed APIs**

The Baggage API exposes a REST API on port 5002. REST was appropriate because baggage operations are resource-oriented CRUD operations on a `Baggage` entity.

[PLACEHOLDER: reference Swagger/OpenAPI specification in Appendix A]

**Consumed and produced events**

The Baggage API publishes events to the `baggagequeue` on RabbitMQ on two occasions: when a baggage item is checked in (`BaggageCheckedIn`) and when its status is updated (`BaggageStatusUpdated`). These events allow other services (or future services) to react to baggage state changes without polling.

**Data storage and ownership**

The Baggage API owns its own PostgreSQL instance, running in a separate Kubernetes namespace (`baggage`) entirely isolated from the main `airport` namespace. The `Baggage` table records booking ID, passenger ID, weight, status, and current location. Status transitions are: `CheckedIn` → `Loaded` → `InTransit` → `Claimed`.

**Scalability and failure considerations**

The Baggage API is stateless with respect to external systems (it does not call other services synchronously), which makes it straightforward to scale horizontally. Its isolated database reduces contention with other services.

---

#### 2.2.6 Assistant Service

**Purpose and responsibilities**

The Assistant Service provides a natural-language question-answering interface for passengers. It was isolated as its own service because LLM inference is computationally heavy compared to other business operations and should not share a process or resource budget with booking or flight management.

**Exposed APIs**

The Assistant Service exposes a REST API via FastAPI:
- `POST /assistant/chat` — accepts a `{ "message": "..." }` body and returns `{ "reply": "..." }`
- `GET /assistant/health` — returns service readiness including whether the model has loaded

The service is proxied through the gateway at the `/assistant` path.

**AI integration**

On each chat request the service fetches the current flight list from the Flight Service's GraphQL API, formats it as a plain-text context string, and injects it into the system prompt before calling the Ollama API. The LLM used is `qwen2.5:3b`, a 3-billion parameter model pulled automatically on first startup and cached in a persistent Docker volume. This context-stuffing approach means the model always has access to live flight data without requiring fine-tuning or a vector database.

**Data storage and ownership**

The Assistant Service maintains no persistent data. Flight data is fetched live from the Flight Service on each request.

**Scalability and failure considerations**

Inference latency for the 3B model is roughly [PLACEHOLDER: measure and insert typical latency] on CPU. Under concurrent load, requests queue behind the Ollama server's single-worker processing. In a production deployment, this would be addressed by running Ollama with GPU acceleration or by routing requests to a managed inference API (e.g. Anthropic, OpenAI) rather than a local model.

---

### 2.3 Communication Between Microservices

Services communicate over two channels:

**Synchronous (REST / GraphQL)**

The frontend communicates with backend services through the gateway. The gateway routes requests to the Flight Service (GraphQL) and to other services as needed. The Assistant Service calls the Flight Service directly via GraphQL HTTP to fetch flight context on each request.

**Asynchronous (RabbitMQ)**

All inter-service events flow through RabbitMQ. The following queues are defined:

| Queue | Producer | Consumer | Message type |
|-------|----------|----------|--------------|
| `Notification` | Flight Service | Notification Service | `NotificationMessage` |
| `payment_queue` | Booking Service | Payment Service | `PaymentMessage` |
| `booking_queue` | Payment Service | Booking Service | `PaymentStatusMessage` |
| `baggagequeue` | Baggage API | (future consumers) | `BaggageCheckedIn`, `BaggageStatusUpdated` |

All queues are declared durable with `autoDelete: false`. Messages are marked persistent. Consumers use manual acknowledgment: messages are only removed from the queue after successful processing.

[PLACEHOLDER: Figure 5 — Insert sequence diagram showing the full booking saga: client → Booking Service → payment_queue → Payment Service → booking_queue → Booking Service status update]

[PLACEHOLDER: reference AsyncAPI documentation in Appendix C for full message schemas]

### 2.4 Patterns and Techniques

#### 2.4.1 CQRS (Command Query Responsibility Segregation)

The Booking Service separates read and write responsibilities into distinct repository interfaces and implementations:

- `IBookingReadRepository` / `BookingReadRepository` — handles queries (`GetByIdAsync`, `GetByUserIdAsync`), using `Include` to eagerly load related passenger records.
- `IBookingWriteRepository` / `BookingWriteRepository` — handles state mutations (`AddAsync`, `UpdateStatusAsync`).

Both repositories operate against the same underlying database, making this a logical CQRS pattern rather than a physical one with separate read and write stores. The separation provides a clear boundary for future evolution: the read model could be moved to a dedicated read replica or a denormalised projection without changing the write side. Dependency injection ensures the service layer receives the appropriate repository for its purpose, preventing read operations from accidentally triggering write paths.

#### 2.4.2 Immutable Data — Tombstone Pattern

The tombstone pattern was planned for the Baggage API but has not been implemented yet. The current Baggage API has no delete endpoint and no soft-delete mechanism: baggage records can be created and have their status updated, but there is no way to mark a record as deleted or to signal deletion to downstream consumers.

The intended implementation would add an `IsDeleted` flag (and optionally a `DeletedAt` timestamp) to the `Baggage` model. A `DELETE /api/baggage/{id}` endpoint would set that flag rather than removing the row. Any RabbitMQ consumer reading from `baggagequeue` would receive a tombstone event (e.g. `BaggageDeleted`) containing only the baggage ID, signalling that the record should be treated as removed. This preserves referential integrity for any service holding a baggage ID and allows consumers to process deletions without encountering missing records.

This is a gap in the current implementation that should be addressed before the system is considered complete.

#### 2.4.3 Idempotence

Booking creation requires a unique combination of user ID and flight ID. If a booking request is re-submitted with the same parameters (e.g. due to a network retry), the database constraint prevents a duplicate record from being created.

Payment status updates are also idempotent in practice: the `BookingEventConsumer` updates the booking's status to the value in the incoming `PaymentStatusMessage`. Receiving the same status message twice leaves the booking in the same state, because `UpdateStatusAsync` is an unconditional write.

[PLACEHOLDER: if explicit idempotency keys or deduplication tables have been added, describe them here]

#### 2.4.4 Commutative Message Handlers

The Notification Service's message handler is commutative in the sense that each `NotificationMessage` is independent: processing message B before message A does not affect the final outcome of either. Each message contains the full payload required to send the email, with no dependency on prior messages or accumulated state. Out-of-order delivery (which RabbitMQ does not guarantee absent persistent queue ordering) cannot produce incorrect results.

#### 2.4.5 Saga Pattern

The booking payment flow is implemented as a choreography-based saga without a central orchestrator:

1. **Booking Service** creates a booking with status `Pending`, then publishes a `PaymentMessage` to `payment_queue` and sets status to `AwaitingPayment`.
2. **Payment Service** consumes the message, calls Stripe, and publishes a `PaymentStatusMessage` to `booking_queue` with the outcome.
3. **Booking Service** consumes the status message and sets the booking to `Confirmed` (payment succeeded) or `Cancelled` (payment failed).

If payment fails the booking is cancelled, which is the compensating action. The saga does not currently handle scenarios where the Booking Service fails to consume the status message (the booking would remain stuck in `AwaitingPayment`); a dead-letter queue and timeout-based cleanup would address this.

[PLACEHOLDER: Figure 5 — Saga sequence diagram]

#### 2.4.6 Caching

[PLACEHOLDER: describe any caching implemented (e.g. in-memory caching for flight queries, response caching at the gateway). If not yet implemented, either remove this section or note it as a planned improvement.]

---

## 3. Environments Description

### 3.1 Development Environment (Docker Compose)

The development environment is defined in `docker-compose.yml` at the repository root. Running `docker compose up --build` starts all services on a single Docker network:

| Service | Image | Port(s) |
|---------|-------|---------|
| postgres | postgres:16-alpine | 5432 |
| baggage-postgres | postgres:16-alpine | 5433 |
| rabbitmq | rabbitmq:3-management | 5672, 15672 (management UI) |
| keycloak | quay.io/keycloak/keycloak:23.0 | 8080 |
| flight | built from `./flight` | 8080 |
| gateway | built from `./gateway` | 5000 |
| booking | built from `./BookingService` | 5001 |
| notification | built from `./NotificationService` | (worker, no HTTP) |
| baggage | built from `./BaggageAPI` | 5002 |
| payment | built from `./PaymentService` | 3000 |
| ollama | ollama/ollama:latest | 11434 |
| assistant | built from `./AssistantService` | (proxied via gateway) |

The Keycloak realm is imported automatically from `keycloak/airport-realm.json` on first startup, creating the `airport-system` realm, client configurations, and default user accounts. Database migrations run automatically when each .NET service starts (EF Core `MigrateAsync` in the startup sequence).

[PLACEHOLDER: Figure 2 — Docker Compose architecture diagram showing service connectivity and port mappings]

**Developer workflow**

A new developer can onboard by:
1. Cloning the monorepo
2. Running `docker compose up --build`
3. Accessing the GraphQL playground at `http://localhost:5000/graphql`
4. Accessing the Keycloak admin console at `http://localhost:8080` (admin/admin)
5. Authenticating as `staff@airport.local` (Staff1234!) or `admin@airport.local` (Admin1234!)

### 3.2 Local Kubernetes Deployment

The Kubernetes environment is configured in the `k8s/` directory and deployed to a local minikube cluster using `k8s/deploy.sh`. It is intended to simulate a production environment, covering service isolation, autoscaling, and observability.

**3.2.1 Technologies Used**

| Component | Technology |
|-----------|-----------|
| Cluster | minikube |
| Container networking | [PLACEHOLDER: CNI used — default or Cilium] |
| Namespaces | `airport` (main services), `baggage` (isolated baggage service) |
| Autoscaling (CPU/memory) | Kubernetes HPA |
| Autoscaling (queue depth) | KEDA |
| Monitoring (metrics) | Prometheus + Grafana |
| Log aggregation | Loki (Grafana stack) |
| Database metrics | Postgres Exporter |
| Identity | Keycloak (NodePort 30880) |
| Gateway (external access) | NodePort 30500 |

Init containers are used in each service deployment to delay application startup until its dependencies (PostgreSQL, RabbitMQ, Keycloak) pass health checks.

[PLACEHOLDER: Figure 3 — Kubernetes architecture diagram showing namespaces, deployments, services, and NodePort access]

**3.2.2 CI/CD Pipeline**

[PLACEHOLDER: A GitHub Actions pipeline is planned but not yet implemented. The pipeline will include the following stages:]

*Planned pipeline structure:*

1. **Static analysis** — run SonarQube or dotnet-format / Roslyn analyzers on each push; fail the build on critical findings
2. **Unit tests** — run `dotnet test` for all .NET test projects; run `npm test` for the Payment Service
3. **Integration tests** — start RabbitMQ and PostgreSQL as service containers; run integration test suites against live infrastructure
4. **System-level cooperation test** — start the Booking Service, Payment Service, and RabbitMQ in containers; submit a booking and assert the booking reaches `Confirmed` status after message exchange
5. **Docker build** — build and tag images for all services
6. **Push to registry** — push images to GitHub Container Registry (or Docker Hub)
7. **Kubernetes manifest validation** — run `kubectl apply --dry-run` against the k8s manifests

Test failures at any stage must fail the pipeline. This ensures no broken code reaches the container registry.

[PLACEHOLDER: Figure 4 — CI/CD pipeline diagram]

**3.2.3 Monitoring and Logging**

The Kubernetes monitoring stack is deployed from the `k8s/monitoring/` manifests and includes:

- **Prometheus** — scrapes metrics from all services and from the Postgres Exporter. Service metrics are exposed on the standard `/metrics` endpoint.
- **Grafana** — provides dashboards for service health, HTTP request rates, queue depths, and database performance.
- **Loki** — aggregates structured logs from all pods. Grafana is configured to query Loki for log search alongside metrics.

[PLACEHOLDER: describe the specific dashboards configured in Grafana and key metrics tracked]

**3.2.4 Autoscaling Configuration**

Two autoscaling strategies are used:

**Booking Service — Horizontal Pod Autoscaler (HPA)**

The Booking Service HPA scales the deployment between 1 and 5 replicas based on resource utilisation:
- CPU threshold: 70% average utilisation triggers scale-out
- Memory threshold: 80% average utilisation triggers scale-out

This is appropriate for an HTTP-serving workload where request concurrency drives CPU and memory usage.

**Notification Service — KEDA ScaledObject**

The Notification Service is scaled by KEDA based on the length of the `notification-queue` in RabbitMQ:
- Scale to zero when the queue is empty (no pods running when there are no notifications to send)
- One new replica per five messages in the queue, up to a maximum of five replicas
- 60-second cooldown before scaling down, to handle bursty traffic without constant pod churn

This approach effectively treats the Notification Service as a serverless function: it consumes no cluster resources when idle and spins up quickly when work arrives.

### 3.3 Cloud Deployment Plan

This section describes how the system would be deployed in a real cloud environment. Actual cloud deployment is not part of this project's deliverables.

[PLACEHOLDER: Figure 7 — Cloud deployment diagram]

**Recommended provider: AWS (or GCP/Azure with analogous services)**

| Component | Cloud service |
|-----------|--------------|
| Kubernetes cluster | Amazon EKS (managed control plane) |
| Database | Amazon RDS for PostgreSQL (Multi-AZ for HA) |
| Message broker | Amazon MQ for RabbitMQ or managed AmazonMQ |
| Container registry | Amazon ECR |
| AI inference | Ollama on a GPU-enabled node group, or replaced by Amazon Bedrock / Anthropic API |
| Identity | Keycloak on EKS, or replaced by AWS Cognito |
| Logging | AWS CloudWatch + Loki, or Datadog |
| Monitoring | Prometheus Managed Service / Grafana Cloud |
| CDN / ingress | AWS ALB (Application Load Balancer) with WAF |

**Elasticity**

EKS node groups can be configured with Cluster Autoscaler to add EC2 nodes under cluster-level pressure. KEDA-based scaling for the Notification Service would function identically on EKS. RDS Multi-AZ provides automatic failover for the database layer. Read replicas would be added to the PostgreSQL instance shared by the Flight and Booking services to separate read load from write operations.

**High availability**

- All stateless services run with at minimum two replicas across availability zones
- RDS Multi-AZ ensures automatic database failover within ~60 seconds
- The message broker is deployed in a cluster configuration (three nodes) so that broker failure does not halt message processing
- Keycloak is deployed in active-active cluster mode backed by a shared database

**Low latency**

- The system is deployed in a single AWS region with multi-AZ distribution to minimise inter-service latency
- The CDN sits in front of static assets
- Database connection pooling (PgBouncer) reduces per-request connection overhead under high concurrency
- The AI Assistant, if backed by a managed inference API, benefits from lower and more predictable latency than a locally-hosted 3B model

**Security and data management**

- All services run in a private VPC subnet; only the gateway and Keycloak are accessible via the public load balancer
- Secrets are stored in AWS Secrets Manager and injected at runtime via the Kubernetes External Secrets Operator
- All traffic within the cluster uses mutual TLS (mTLS) via a service mesh (e.g. AWS App Mesh or Istio)
- Database encryption at rest is enabled on all RDS instances
- S3 bucket policies and IAM roles follow the principle of least privilege

**Pricing strategy**

[PLACEHOLDER: estimate compute costs based on expected request volume and replica counts. For a medium-traffic airport system (~1000 concurrent users), rough estimates for AWS: EKS cluster ~$150/month for control plane, 3x m5.large nodes ~$300/month, RDS db.t3.medium Multi-AZ ~$130/month, managed RabbitMQ ~$150/month]

**Deployment and release approach**

- Blue/green deployments via Kubernetes rolling updates, ensuring zero-downtime releases
- Database migrations are run as Kubernetes Jobs (init jobs) before service pods start, preventing schema/code mismatches
- Feature flags (e.g. AWS AppConfig or LaunchDarkly) allow gradual rollout of new functionality

---

## 4. Testing

### 4.1 Testing Strategy and Scope

The testing strategy targets correctness at three levels: individual service logic (unit tests), service integrations with infrastructure (integration tests), and multi-service interaction via messaging (cooperation tests). Static analysis is run to catch code quality and security issues before they reach review.

The scope focuses on backend business logic and service communication. UI testing is out of scope for this project.

### 4.2 Unit Testing

Unit tests are written using xUnit and Moq. Each backend service has a dedicated test project:

| Test project | Service | Framework |
|-------------|---------|-----------|
| `AirportSystem.Tests` | Flight Service | xUnit + Moq |
| `BookingService.Tests` (whitebox) | Booking Service | xUnit + Moq |

**Flight Service unit tests** cover `FlightService`, `GateService`, and `UserSyncService`. Tests mock `AppDbContext` using an in-memory EF Core provider. Example coverage:

- Creating a flight with valid/invalid date ranges
- Updating flight status and triggering event publisher mock
- Following and unfollowing flights, including duplicate follow detection

**Booking Service whitebox tests** mock `IBookingReadRepository`, `IBookingWriteRepository`, and `IBookingEventPublisher`. They test the service layer's booking creation, validation, and status transition logic in isolation from infrastructure.

### 4.3 Integration Testing

Integration tests run against real RabbitMQ and PostgreSQL instances started as containers. The integration test suite is located in `BookingService.Tests/Integration/` and `NotificationTest/`.

| Test | What it validates |
|------|------------------|
| `RabbitMqIntegrationTests` (Booking) | A `PaymentStatusMessage` published to `booking_queue` causes the Booking Service consumer to update the booking's status in the database |
| `NotificationTest/RabbitMqTest` | A `NotificationMessage` published to the `Notification` queue is consumed by the Notification Service and dispatched to `IMailSender` |

These tests verify that the asynchronous communication contracts between services work against live infrastructure, not just against mocks.

Integration tests for REST API behaviour are covered in the blackbox test suite (`BookingService.Tests/Blackbox/`), which submits HTTP requests to a running Booking Service instance and asserts the response codes and payloads.

### 4.4 System-Level Cooperation Testing

[PLACEHOLDER: describe the system-level cooperation test that verifies the full booking-payment saga. The test should:
1. Start Booking Service, Payment Service, and RabbitMQ as containers
2. Submit a booking via the Booking Service REST API
3. Assert that the `payment_queue` receives a `PaymentMessage`
4. Assert that the Payment Service publishes a `PaymentStatusMessage` to `booking_queue`
5. Assert that the Booking Service's booking status transitions to `Confirmed`

If this test is not yet implemented, note it here as a gap and describe the intended approach.]

### 4.5 Security Testing

Authentication and authorisation behaviour is tested as part of the blackbox and whitebox test suites:

- Requests without a valid JWT are rejected at the gateway with HTTP 401
- Requests with a valid JWT but insufficient role (e.g. a Passenger attempting a Staff-only mutation) are rejected with HTTP 403
- Role claims are extracted from the JWT and forwarded via `X-User-Roles` headers; tests verify that the flight service correctly enforces `@Authorize` role restrictions at the resolver level

[PLACEHOLDER: if additional security tests have been written (e.g. testing for injection in booking inputs, verifying that one user cannot read another user's bookings), describe them here]

### 4.6 Static Analysis and Static Testing

[PLACEHOLDER: describe the static analysis tools used. Recommended options for .NET services: Roslyn analyzers (built into SDK), SonarQube / SonarCloud, or dotnet-format. For the Node.js Payment Service: ESLint with security plugins. Note which rules are enforced and what severity level fails the build.]

### 4.7 Test Execution in CI/CD

[PLACEHOLDER: once the CI/CD pipeline is implemented, describe how tests are triggered. All tests should run on every pull request to `main`. Test failures must fail the pipeline and block the merge.]

**Stress and load testing** is handled separately using k6, located in `StressTests/`. The suite includes five test profiles for the Booking Service:

| Script | Profile |
|--------|---------|
| `booking-load-low.k6.js` | Baseline — steady low traffic |
| `booking-load-mid.k6.js` | Medium load |
| `booking-load-high.k6.js` | High sustained load |
| `booking-spike.k6.js` | Sudden traffic spike |
| `booking-stress.k6.js` | Stress test to find breaking point |

[PLACEHOLDER: insert k6 result summaries — p95 latency, throughput, error rates under each profile]

### 4.8 Limitations, Risks, and Trade-offs

- The unit tests for the Flight Service use an in-memory EF Core provider. In-memory providers do not enforce relational constraints, so tests that rely on foreign key violations or unique constraints will not catch those bugs at the unit test level.
- The current saga implementation has no dead-letter queue or timeout for bookings stuck in `AwaitingPayment`. A network partition between the Payment Service and RabbitMQ would leave bookings unresolved indefinitely.
- Load testing targets only the Booking Service. The Flight Service's GraphQL subscription performance under concurrent WebSocket connections has not been measured.

---

## 5. Project Management and Team Collaboration

### 5.1 Introduction

[INSERT: describe the team structure, how work was divided between members, and the overall timeline of the project.]

### 5.2 Methods Used

[INSERT: describe the project management approach — e.g. Scrum sprints, Kanban board, weekly stand-ups, code review process, pull request workflow. Reference the GitHub issue tracker or project board if used.]

### 5.3 Versioning Strategies

**Source code versioning**

All source code is maintained in a GitHub monorepo. Each microservice resides in its own top-level directory. Feature work is developed on feature branches and merged to `main` via pull requests. Branch naming follows the convention `feature/<description>` or `fix/<description>`. Contributions from each group member are visible in the git commit history.

**Database versioning**

All .NET services use EF Core code-first migrations. Each migration is a timestamped C# class checked into the repository alongside the service code. Migrations run automatically on service startup via `MigrateAsync()`. This ensures the database schema is always in sync with the deployed service version. Migration files are treated as immutable once merged to `main`; schema changes always create new migration files rather than modifying existing ones.

**API versioning**

REST APIs are versioned via URL path prefix: `/api/v1/...`. This allows a new version of an endpoint to be introduced without breaking existing clients. The current API version for all REST services is v1.

GraphQL does not require versioning by design: the schema evolves additively (new fields and types are added without removing existing ones), and clients select only the fields they need.

### 5.4 Documentation Strategy

Documentation is maintained at three levels:

- **GitHub README** — each service directory contains a README explaining the service's purpose, how to run it locally, and links to its API documentation
- **Monorepo README** — the root README provides an overview of the system, a quickstart guide, and links to each service
- **API documentation** — REST APIs are documented via Swagger/OpenAPI (accessible at `/swagger` when running in development mode); the GraphQL schema is self-documenting via introspection and available in the Banana Cake Pop playground
- **AsyncAPI** — message contracts for RabbitMQ queues are documented in AsyncAPI format in Appendix C

---

## 6. Discussion

### 6.1 Advantages and Challenges of Distributed Systems

**Application perspective**

The microservices architecture provides clear advantages in this system: the Notification Service can be deployed and updated independently of the Flight Service, and the Payment Service can be swapped for a different payment provider without touching booking logic. Independent deployability reduces the blast radius of any single deployment.

The main challenge is operational complexity. What would be a function call in a monolith becomes a network call with latency, potential failure, and serialisation overhead. The booking saga, for instance, requires careful reasoning about failure modes at each message exchange that simply would not arise in a single-process design.

**Database perspective**

Separate databases per service eliminate cross-service join queries, which forces services to be explicit about what data they own and what they need from others. The trade-off is that queries that would naturally span tables (e.g. finding all bookings for a given flight with passenger details) must be assembled from multiple service calls rather than a single SQL query.

The Baggage API's completely isolated PostgreSQL instance demonstrates strong ownership but introduces operational cost: two PostgreSQL instances must be maintained, monitored, and backed up separately.

### 6.2 Pros and Cons of Applied Patterns

**CQRS**

Separating read and write repositories in the Booking Service makes the code easier to reason about: a method that queries bookings cannot accidentally write to them. It also provides a foundation for introducing a read replica for query traffic. The downside is additional interface definitions for what is currently a relatively simple service — the benefit only becomes obvious as the read and write models diverge.

**Saga (choreography-based)**

The choreography-based saga requires no central orchestrator, which reduces coupling. Each service only knows about the messages it publishes and consumes. The risk is that the overall flow is implicit: to understand the full booking lifecycle you have to trace through three separate services. An orchestrated saga would make the flow explicit at the cost of a new orchestrator service.

**Tombstone pattern**

Keeping cancelled flights as tombstone records prevents dangling references in services that store flight IDs (like the Booking Service). The cost is that queries for active flights must filter out cancelled records, and the database grows unboundedly. For an airport context the growth rate is manageable; for high-volume systems a time-based archival strategy would complement the pattern.

### 6.3 Scalability and Autoscaling

**Application scaling**

The HPA on the Booking Service handles HTTP request spikes automatically. The KEDA-based scaling on the Notification Service is particularly effective because email sending is a bursty workload — there is no value in keeping replicas running during quiet periods, and the queue-depth trigger ensures replicas appear as soon as work arrives.

**Messaging scaling**

RabbitMQ itself is a potential bottleneck. The current single-node setup would need to be replaced with a clustered RabbitMQ deployment or migrated to Amazon MQ before supporting production-level traffic. The message contracts are already well-defined, so a broker migration would not require changes to the services.

**Database scaling**

Read replicas and connection pooling (PgBouncer) would be the first scaling steps for PostgreSQL. The Booking Service's CQRS separation means that read traffic can be directed to a replica with a configuration change rather than a code change.

### 6.4 Possible Improvements

1. Add a dead-letter queue and a cleanup job for bookings stuck in `AwaitingPayment` past a timeout threshold.
2. Replace the Flight Service's in-memory subscription topic with a Redis-backed provider so subscriptions work correctly when the service is scaled to multiple replicas.
3. Implement circuit breakers (e.g. Polly in .NET) on inter-service HTTP calls so that a slow or unresponsive Flight Service does not stall the AI Assistant Service indefinitely.
4. Add a proper API versioning strategy with Swagger UI distinguishing between v1 and any future v2 endpoints.
5. Implement an automated cloud deployment pipeline targeting AWS EKS to remove the manual deployment step.
6. Introduce a service mesh (e.g. Istio) to enforce mTLS between services and gain observability into inter-service latency without modifying application code.

---

## 7. Reflection

**Designing the distributed system**

The most difficult design decision was choosing between synchronous and asynchronous communication for each service interaction. The booking-to-payment flow was initially designed as a synchronous HTTP call from the Booking Service to the Payment Service, which created tight coupling and meant that Payment Service downtime would block the entire booking flow. Switching to RabbitMQ-based messaging resolved this but introduced the need to think carefully about message durability, consumer idempotency, and what to do when messages are not acknowledged.

The GraphQL subscription implementation for real-time flight updates required understanding how HotChocolate manages in-memory topic channels and their limitations in multi-replica deployments. This was not obvious from the documentation and required experimentation.

**Project management**

[INSERT: describe specific challenges in coordinating the team, e.g. merge conflicts on shared models, coordinating the integration of services built by different team members, managing the Keycloak configuration as a shared dependency.]

**Design mistakes made and fixed**

[INSERT: describe specific mistakes discovered and corrected during development, e.g. the auth bugs noted in the project history (Newtonsoft JObject serialisation, issuer mismatch), database schema decisions that required migration revisions, or messaging contracts that changed after initial implementation.]

**What would be done differently**

In hindsight, defining the AsyncAPI message contracts at the start of the project (before writing consumers and producers) would have caught schema mismatches earlier. Several adjustments to message payloads required coordinated changes across two services because the contract had not been written down.

Setting up the CI/CD pipeline earlier rather than treating it as a deliverable would also have caught integration problems sooner and reduced the time spent debugging environment-specific issues.

**What was learned about databases in practice**

Running multiple PostgreSQL instances and managing EF Core migrations across separate services made it clear that database versioning is as important as code versioning. The EF Core migration approach works well for a single service but requires discipline to ensure migrations are never modified after merging to main. The `.Designer.cs` snapshot files can cause conflicts when multiple developers add migrations in parallel on separate branches.

---

## 8. Conclusion

This project demonstrates a distributed airport management system built across six backend microservices, an API gateway, and supporting infrastructure. The system covers the key properties of large-scale distributed systems: independent deployability of each service, asynchronous event-driven communication via RabbitMQ, horizontal autoscaling via Kubernetes HPA and KEDA, and security enforced through Keycloak JWT validation at the gateway layer.

The implementation shows several distributed systems patterns in practice: CQRS in the Booking Service, the tombstone approach to flight lifecycle management, a choreography-based saga for the booking-payment flow, and event-driven serverless scaling for the Notification Service. The AI Assistant Service demonstrates that local LLM inference can be integrated into a production-style workflow without dependency on external paid APIs, by running Ollama in a container alongside the rest of the system.

The main outstanding gap is the CI/CD pipeline, which is designed but not yet implemented. Once in place, it will enforce that all tests pass before any change is merged, completing the automated quality gate across the full delivery pipeline.

---

## 9. References

[INSERT: add references in APA, IEEE, or Harvard format for all technical claims, design choices, and comparisons. Recommended starting points:]

- Newman, S. (2021). *Building Microservices: Designing Fine-Grained Systems* (2nd ed.). O'Reilly Media.
- Richardson, C. (2018). *Microservices Patterns: With Examples in Java*. Manning Publications.
- Fowler, M., & Lewis, J. (2014). Microservices. Retrieved from https://martinfowler.com/articles/microservices.html
- HotChocolate documentation. (n.d.). ChilliCream. Retrieved from https://chillicream.com/docs/hotchocolate
- YARP documentation. (n.d.). Microsoft. Retrieved from https://microsoft.github.io/reverse-proxy/
- KEDA documentation. (n.d.). KEDA project. Retrieved from https://keda.sh/docs/
- Keycloak documentation. (n.d.). Red Hat. Retrieved from https://www.keycloak.org/documentation
- Kleppmann, M. (2017). *Designing Data-Intensive Applications*. O'Reilly Media.
- Kubernetes documentation. (n.d.). Cloud Native Computing Foundation. Retrieved from https://kubernetes.io/docs/
- Ollama documentation. (n.d.). Retrieved from https://ollama.com/

---

## 10. Appendix

### Appendix A: OpenAPI / Swagger Specifications

[PLACEHOLDER: embed or reference the Swagger JSON/YAML for the Booking Service and Baggage API REST endpoints. These can be exported from the running development environment at `/swagger/v1/swagger.json`.]

### Appendix B: GraphQL Schema

[PLACEHOLDER: embed the Flight Service GraphQL schema. Export via introspection from the Banana Cake Pop playground or via `dotnet graphql` tooling.]

### Appendix C: AsyncAPI Message Documentation

[PLACEHOLDER: embed the AsyncAPI YAML document describing all RabbitMQ queues, message schemas, producers, and consumers. Template below:]

```yaml
asyncapi: '2.6.0'
info:
  title: Airport System Messaging API
  version: '1.0.0'

channels:
  Notification:
    subscribe:
      operationId: sendNotificationEmail
      message:
        $ref: '#/components/messages/NotificationMessage'
  payment_queue:
    subscribe:
      operationId: processPayment
      message:
        $ref: '#/components/messages/PaymentMessage'
  booking_queue:
    subscribe:
      operationId: updateBookingStatus
      message:
        $ref: '#/components/messages/PaymentStatusMessage'
  baggagequeue:
    subscribe:
      operationId: processBaggageEvent
      message:
        oneOf:
          - $ref: '#/components/messages/BaggageCheckedIn'
          - $ref: '#/components/messages/BaggageStatusUpdated'

components:
  messages:
    NotificationMessage:
      payload:
        type: object
        properties:
          fromName: { type: string }
          toEmail: { type: string }
          subject: { type: string }
          body: { type: string }
    PaymentMessage:
      payload:
        type: object
        properties:
          bookingId: { type: string, format: uuid }
          flightNumber: { type: string }
          seatClass: { type: string }
          totalPrice: { type: number }
          contactEmail: { type: string }
    PaymentStatusMessage:
      payload:
        type: object
        properties:
          bookingId: { type: string, format: uuid }
          paymentSucceeded: { type: boolean }
```

### Appendix D: Keycloak Realm Configuration

The full Keycloak realm configuration is available at `keycloak/airport-realm.json` in the repository. It defines the `airport-system` realm, the `airport-api` and `airport-frontend` clients, role mappings, and default user accounts for development.

### Appendix E: k6 Stress Test Results

[PLACEHOLDER: run the k6 test scripts in `StressTests/` and paste the summary output for each profile here. Include at minimum: virtual users, duration, requests/second, p95 response time, and error rate.]
