# Local Dev Architecture — docker-compose

> All services run in a single Docker network. Host port mappings shown on entry points.

```mermaid
graph TD
    %% ── Host entry points ─────────────────────────────────────────────────────
    DEV(["Developer "])

    subgraph PORTS["Host ports"]
        direction LR
        P_GW["Gateway\nlocalhost:5000"]
        P_BK["Booking\nlocalhost:5001"]
        P_BAG["Baggage\nlocalhost:5003"]
        P_PAY["Payment\nlocalhost:3000"]
        P_KC["Keycloak\nlocalhost:8080"]
        P_RMQ["RabbitMQ UI\nlocalhost:15672"]
        P_OL["Ollama\nlocalhost:11434"]
        P_PG["Postgres\nlocalhost:5432"]
    end

    DEV --> P_GW & P_BK & P_BAG & P_PAY & P_KC & P_RMQ

    %% ── docker-compose network ────────────────────────────────────────────────
    subgraph DC["docker-compose network"]

        subgraph SVCS["Application Services"]
            GW["gateway"]
            FLIGHT["flight"]
            BOOKING["booking"]
            BAGGAGE["baggage"]
            PAYMENT["payment"]
            NOTIF["notification\n(no host port)"]
            ASSIST["assistant\n(no host port)"]
        end

        subgraph INFRA["Infrastructure"]
            KC["keycloak\n:8080"]
            RMQ[("rabbitmq\n:5672 / :15672")]
            OLLAMA["ollama\n:11434"]
        end

        subgraph DBS["Databases"]
            PG[("postgres\nairport_system\n:5432")]
            MYSQL[("mysql\n:3306")]
        end

    end

    %% ── Port bindings ─────────────────────────────────────────────────────────
    P_GW  --> GW
    P_BK  --> BOOKING
    P_BAG --> BAGGAGE
    P_PAY --> PAYMENT
    P_KC  --> KC
    P_RMQ --> RMQ
    P_OL  --> OLLAMA
    P_PG  --> PG

    %% ── Gateway depends on ────────────────────────────────────────────────────
    GW -->|depends_on| FLIGHT
    GW -->|JWT validate| KC

    %% ── Service → Postgres ────────────────────────────────────────────────────
    FLIGHT  -. SQL\nairport_system .-> PG
    BOOKING -. SQL\nairport_system .-> PG
    BAGGAGE -. SQL\nbaggage_db .-> PG
    KC      -. SQL\nairport_system .-> PG

    %% ── Service → RabbitMQ ────────────────────────────────────────────────────
    FLIGHT  -- AMQP --> RMQ
    BOOKING -- AMQP --> RMQ
    BAGGAGE -- AMQP --> RMQ
    PAYMENT -- AMQP --> RMQ
    NOTIF   -- AMQP --> RMQ

    %% ── Service → MySQL ───────────────────────────────────────────────────────
    PAYMENT -. SQL .-> MYSQL

    %% ── Auth ──────────────────────────────────────────────────────────────────
    FLIGHT  -->|JWT| KC
    BOOKING -->|JWT| KC

    %% ── Assistant ─────────────────────────────────────────────────────────────
    ASSIST -->|GraphQL| FLIGHT
    ASSIST -->|LLM API| OLLAMA

    %% ── Styles ────────────────────────────────────────────────────────────────
    classDef svc  fill:#dbeafe,stroke:#3b82f6,color:#1e3a5f
    classDef db   fill:#fef9c3,stroke:#ca8a04,color:#713f12
    classDef infr fill:#dcfce7,stroke:#16a34a,color:#14532d
    classDef ext  fill:#f3f4f6,stroke:#6b7280,color:#111827

    class GW,FLIGHT,BOOKING,BAGGAGE,PAYMENT,NOTIF,ASSIST svc
    class PG,MYSQL db
    class KC,RMQ,OLLAMA infr
    class DEV,P_GW,P_BK,P_BAG,P_PAY,P_KC,P_RMQ,P_OL,P_PG ext
```

## Startup order

```
mysql ─────────────────────────────────────────────────────► payment
postgres ──► keycloak ──► flight ──► gateway
         └──────────────► booking
         └──────────────► baggage
rabbitmq ──► flight, booking, baggage, payment, notification
ollama ─────────────────────────────────────────────────────► assistant
```

