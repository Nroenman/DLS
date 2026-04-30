# Kubernetes Local Setup

Runs the full Airport System on a local minikube cluster — a close mirror of the docker-compose setup, split across two namespaces.

| Namespace | Services |
|---|---|
| `airport` | postgres, rabbitmq, keycloak, flight, booking, notification, gateway |
| `baggage` | postgres, rabbitmq, baggage-api |

---

## Prerequisites

- minikube

- kubectl

---

## First-time setup

**1. Start minikube** with enough resources for all services:

```bash
minikube start --cpus 4 --memory 6144 --disk-size 20g
```

**2. Deploy the application:**

```bash
bash k8s/deploy.sh
```

This will:

- Point Docker at minikube's daemon (Should already be the case but just to be sure)
- Build all service images directly into minikube (no registry needed)
- Create the namespaces
- Load the Keycloak realm config from `keycloak/airport-realm.json`
- Apply all manifests in dependency order

**3. (Optional) Deploy the monitoring stack:**

```bash
bash k8s/monitoring/install.sh
```

Installs Prometheus, Grafana, Loki, and Promtail via Helm, plus a postgres exporter for metrics.

---

## Access points

After deployment, get the minikube IP with `minikube ip` and use the ports below:

| Service | URL | Credentials |
|---|---|---|
| Gateway (API entry point) | `http://<minikube-ip>:30500` | — |
| Keycloak admin console | `http://<minikube-ip>:30880` | admin / admin |
| RabbitMQ management UI | `http://<minikube-ip>:30672` | guest / guest |
| Baggage API | `http://<minikube-ip>:30501` | — |
| Grafana (if monitoring installed) | `http://<minikube-ip>:30300` | admin / admin |

Or let minikube open a service directly:

```bash
minikube service gateway -n airport --url
```

---

## Watching the rollout

Services start in dependency order via init containers (postgres and rabbitmq must be healthy before app services start, keycloak before gateway). Watch progress with:

```bash
kubectl get pods -n airport -w
kubectl get pods -n baggage -w
```

Keycloak takes the longest (~60–90s) as it imports the realm on first boot.

---

## Rebuilding after code changes

Re-run `deploy.sh` — it rebuilds images and re-applies manifests. To rebuild a single service:

```bash
eval "$(minikube docker-env --shell bash)"
docker build -t airport/flight:local -f flight/src/AirportSystem.Flights/Dockerfile ./flight
kubectl rollout restart deployment/flight -n airport
```

---

## Teardown

```bash
minikube delete
```
