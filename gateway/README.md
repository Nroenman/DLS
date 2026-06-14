# Gateway

YARP reverse proxy (.NET 8). The sole public-facing entry point for all API traffic. Validates JWTs against Keycloak, extracts user identity and roles from the token, and forwards requests to the appropriate downstream service with trusted identity headers.

---

## Routing

Routes are matched top-to-bottom. More-specific paths are listed first; the catch-all at the bottom forwards everything else to the flight service.

| Path prefix | Downstream service |
|-------------|-------------------|
| `/api/Booking/**` | BookingService `:8080` |
| `/api/payment/**` | PaymentService `:3001` |
| `/api/v1/baggage/**` | BaggageAPI `:8080` |
| `/assistant/**` | AssistantService `:8080` |
| `/**` (catch-all) | Flight service `:8080` (includes `/graphql`) |

Routes and cluster addresses are configured in [`appsettings.json`](appsettings.json) under `ReverseProxy`. No code changes are needed to add a new service — add a `Route` + `Cluster` entry there.

### Adding a new service

```jsonc
// appsettings.json → ReverseProxy
"Routes": {
  "my-service": {                         // add before the catch-all
    "ClusterId": "my-service",
    "Match": { "Path": "/api/myservice/{**catch-all}" }
  },
  "airport-api": { ... }                  // catch-all stays last
},
"Clusters": {
  "my-service": {
    "Destinations": {
      "primary": { "Address": "http://myservice:8080/" }
    }
  }
}
```

---

## JWT validation

The gateway validates every `Authorization: Bearer` token before forwarding:

- Public key fetched from Keycloak's JWKS endpoint via `options.Authority`
- Issuer validation is **disabled** — tokens carry the browser-facing Keycloak URL (`http://localhost:8080`) but the gateway resolves Keycloak by its Docker/k8s service name; the two differ in most deployments
- Audience validation is disabled — clients use the `airport-frontend` client ID
- Signature validation is the actual trust boundary

Requests with **no token** pass through unchanged — downstream services enforce their own field-level authorization rules (e.g. `[Authorize(Roles = "Staff")]` on GraphQL mutations).

Requests with an **invalid or expired token** are rejected with `401` at the gateway.

---

## Trusted identity headers

When a token is valid the gateway decodes the JWT payload directly and forwards identity as trusted HTTP headers. Downstream services read these headers without re-validating the token.

| Header | Source |
|--------|--------|
| `X-User-Id` | `sub` claim |
| `X-User-Email` | `email` claim |
| `X-User-Name` | `preferred_username` claim |
| `X-User-Roles` | `realm_access.roles` — comma-separated, e.g. `Passenger,Staff` |

Services that receive no `X-User-Id` treat the request as unauthenticated.

---

## Environment variables

| Variable | Default | Description |
|----------|---------|-------------|
| `Keycloak__BaseUrl` | — | Keycloak base URL, e.g. `http://keycloak:8080` |
| `Keycloak__Realm` | — | Realm name, e.g. `airport-system` |
| `ASPNETCORE_ENVIRONMENT` | `Production` | Set to `Development` for verbose logging |

---

## Running locally

```bash
cd gateway
DOTNET_ROLL_FORWARD=Major dotnet run
```

The gateway listens on port `8080` by default. In Docker Compose it is accessible directly at `http://localhost:5000`.
