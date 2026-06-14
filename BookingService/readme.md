# BookingService

## Features

- Create bookings (one-way and return)
- Retrieve booking information by booking ID
- Retrieve all bookings for a specific user
- Update booking status
- Cancel bookings
- REST API endpoints
- Swagger/OpenAPI documentation
- Entity Framework Core data access
- PostgreSQL database integration
- RabbitMQ messaging for payment and notification events
- Keycloak authentication on protected endpoints

## Technologies

- ASP.NET Core Web API (.NET 9)
- Entity Framework Core
- PostgreSQL
- RabbitMQ
- Keycloak
- Swagger / OpenAPI
- xUnit (Testing)
- Moq (Mocking)
- Docker

## Architecture

The service follows a layered architecture:

```
Controller Layer
       │
Service Layer
       │
Validator Layer
       │
Repository Layer
       │
Database
```

Messaging is handled by a separate `Messaging` layer containing the event publisher and the background consumer.

## Authentication

Protected endpoints are guarded by a custom `KeycloakAuthenticationMiddleware` that validates JWT tokens via JWKS and performs token introspection against Keycloak.

Endpoints requiring authentication:

- `POST /api/booking`
- `GET /api/booking/user/{userId}`
- `PUT /api/booking/{id}/cancel`

## Messaging

The service communicates with other microservices through RabbitMQ:

- **Publishes to** `payment_queue` — when a booking is created and requires payment
- **Publishes to** `notification_queue` — when a booking is created or cancelled
- **Consumes from** `booking_queue` — handled by `BookingEventConsumer` (a `BackgroundService`)



## Testing Strategy

- Black-box tests cover validator boundary logic using BVA, equivalence partitioning, and decision tables.
- White-box tests directly exercise `BookingValidator` with mocked repositories.
- Integration tests cover database operations (PostgreSQL) and RabbitMQ message publishing.

---

## Example Endpoints

### Create Booking

```http
POST /api/booking
```

### Get Booking by ID

```http
GET /api/booking/{id}
```

### Get Bookings by User ID

```http
GET /api/booking/user/{userId}
```

### Update Booking Status

```http
PUT /api/booking/{id}/status
```

### Cancel Booking

```http
PUT /api/booking/{id}/cancel
```