# Kubernetes Architecture — AirportSystem

> **Namespace:** `airport`  
> Autoscaling via HPA (CPU 70% / Memory 80%) unless noted. Notification scales via KEDA on queue depth.

```mermaid
graph TD
    %% ── External ──────────────────────────────────────────────────────────────
    CLIENT(["Client "])

    subgraph NODEPORTS["NodePorts (external access)"]
        direction LR
        NP_GW["Gateway\n:30500"]
        NP_BAG["Baggage\n:30501"]
        NP_KC["Keycloak\n:30880"]
        NP_RMQ["RabbitMQ UI\n:30672"]
    end

    CLIENT --> NP_GW & NP_BAG & NP_KC

    %% ── Namespace: airport ────────────────────────────────────────────────────
    subgraph K8S["namespace: airport"]

        %% Entry point
        subgraph ENTRY["Entry Point"]
            GW["Gateway\nHPA 1–3"]
        end

        %% Business services
        subgraph SVCS["Application Services"]
            FLIGHT["Flight\nHPA 1–5"]
            BOOKING["Booking\nHPA 1–5"]
            BAGGAGE["Baggage\nHPA 1–5"]
            PAYMENT["Payment\nHPA 1–3"]
            NOTIF["Notification\nKEDA 0–5"]
            ASSIST["Assistant\nHPA 1–2"]
        end

        %% Shared infrastructure
        subgraph INFRA["Shared Infrastructure"]
            RMQ[("RabbitMQ\namqp :5672\nmgmt :15672")]
            KC["Keycloak\n:8080"]
            OLLAMA["Ollama\n:11434"]
            MYSQL[("MySQL\n:3306")]
        end

        %% Per-service databases
        subgraph DBS["Per-Service Databases (PostgreSQL)"]
            FLIGHT_PG[("flight-postgres\n:5432")]
            BOOKING_PG[("booking-postgres\n:5432")]
            BAGGAGE_PG[("baggage-postgres\n:5432")]
            KC_PG[("keycloak-postgres\n:5432")]
        end

        %% Monitoring
        subgraph MON["Monitoring (prometheus-stack + loki-stack)"]
            PROM["Prometheus\n+ Grafana"]
            PG_EXP["postgres-exporter\n:9187"]
            LOKI["Loki\n:3100\n(in-memory, 24h)"]
            PROMTAIL["Promtail\n(DaemonSet)"]
        end

    end

    %% ── NodePort → Service ────────────────────────────────────────────────────
    NP_GW  --> GW
    NP_BAG --> BAGGAGE
    NP_KC  --> KC
    NP_RMQ --> RMQ

    %% ── Gateway routing ───────────────────────────────────────────────────────
    GW -->|HTTP| FLIGHT
    GW -->|HTTP| BOOKING
    GW -->|HTTP| BAGGAGE
    GW -->|HTTP| PAYMENT
    GW -->|HTTP| ASSIST
    GW -->|JWT validate| KC

    %% ── Service → Database ────────────────────────────────────────────────────
    FLIGHT  -. SQL .-> FLIGHT_PG
    BOOKING -. SQL .-> BOOKING_PG
    BAGGAGE -. SQL .-> BAGGAGE_PG
    PAYMENT -. SQL .-> MYSQL
    KC      -. SQL .-> KC_PG

    %% ── Service → RabbitMQ ────────────────────────────────────────────────────
    FLIGHT  -- AMQP --> RMQ
    BOOKING -- AMQP --> RMQ
    BAGGAGE -- AMQP --> RMQ
    PAYMENT -- AMQP --> RMQ
    NOTIF   -- AMQP --> RMQ

    %% ── Auth ──────────────────────────────────────────────────────────────────
    FLIGHT  -->|JWT| KC
    BOOKING -->|JWT| KC

    %% ── Assistant ─────────────────────────────────────────────────────────────
    ASSIST -->|GraphQL| FLIGHT
    ASSIST -->|LLM API| OLLAMA

    %% ── Monitoring scrape targets ─────────────────────────────────────────────
    PG_EXP  -. SQL .-> FLIGHT_PG
    PROM    -->|scrape :9187| PG_EXP
    PROM    -->|scrape /metrics| FLIGHT
    PROM    -->|scrape /metrics| BOOKING
    PROM    -->|scrape /metrics| GW
    PROM    -->|scrape :15692| RMQ
    PROMTAIL -->|pod logs| LOKI
    PROM    -->|query logs| LOKI

    %% ── Styles ────────────────────────────────────────────────────────────────
    classDef svc  fill:#dbeafe,stroke:#3b82f6,color:#1e3a5f
    classDef db   fill:#fef9c3,stroke:#ca8a04,color:#713f12
    classDef infr fill:#dcfce7,stroke:#16a34a,color:#14532d
    classDef mon  fill:#fce7f3,stroke:#db2777,color:#831843
    classDef ext  fill:#f3f4f6,stroke:#6b7280,color:#111827

    class FLIGHT,BOOKING,BAGGAGE,PAYMENT,NOTIF,ASSIST,GW svc
    class FLIGHT_PG,BOOKING_PG,BAGGAGE_PG,KC_PG,MYSQL db
    class RMQ,KC,OLLAMA infr
    class PROM,PG_EXP,LOKI,PROMTAIL mon
    class CLIENT,NP_GW,NP_BAG,NP_KC,NP_RMQ ext
```

## Resource summary

| Service | Replicas | Scales on | Public |
|---|---|---|---|
| Gateway | 1–3 | CPU / Memory | NodePort :30500 |
| Flight | 1–5 | CPU / Memory | via Gateway |
| Booking | 1–5 | CPU / Memory | via Gateway |
| Baggage | 1–5 | CPU / Memory | NodePort :30501 |
| Payment | 1–3 | CPU / Memory | via Gateway |
| Notification | 0–5 | **KEDA** queue depth | — |
| Assistant | 1–2 | CPU / Memory | via Gateway |
| Keycloak | 1 | — | NodePort :30880 |
| RabbitMQ | 1 | — | Mgmt NodePort :30672 |
| Ollama | 1 | — | — |
| MySQL | 1 | — | — |
| *-postgres | 1 each | — | — |
