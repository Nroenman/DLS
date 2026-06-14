#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

# Use minikube's bundled kubectl if kubectl is not on PATH
if ! command -v kubectl &>/dev/null; then
  kubectl() { minikube kubectl -- "$@"; }
fi

# ── 1. Point Docker at minikube's daemon ────────────────────────────────────
echo "==> Configuring Docker to use minikube's daemon..."
eval "$(minikube docker-env --shell bash)"

# ── 2. Build images ──────────────────────────────────────────────────────────
echo "==> Building images..."
docker build -t airport/flight:local    -f "$PROJECT_ROOT/flight/src/AirportSystem.Flights/Dockerfile"  "$PROJECT_ROOT/flight"
docker build -t airport/gateway:local   "$PROJECT_ROOT/gateway"
docker build -t airport/booking:local   "$PROJECT_ROOT/BookingService"
docker build -t airport/notification:local "$PROJECT_ROOT/NotificationService"
docker build -t airport/baggage:local   "$PROJECT_ROOT/BaggageAPI"
docker build -t airport/payment:local   "$PROJECT_ROOT/PaymentService"
docker build -t airport/assistant:local "$PROJECT_ROOT/AssistantService"

# Pull Ollama into minikube's daemon so the cluster can run it offline.
docker pull ollama/ollama:latest

# ── 3. Enable minikube addons ────────────────────────────────────────────────
echo "==> Enabling metrics-server..."
minikube addons enable metrics-server

# ── 4. Install KEDA (queue-based autoscaling for notification service) ───────
echo "==> Installing KEDA..."
helm repo add kedacore https://kedacore.github.io/charts
helm repo update kedacore
helm upgrade --install keda kedacore/keda \
  --namespace keda --create-namespace \
  --wait --timeout 3m

# ── 5. Create namespaces ─────────────────────────────────────────────────────
echo "==> Creating namespaces..."
kubectl apply -f "$SCRIPT_DIR/namespace.yaml"

# ── 6. Create Keycloak realm ConfigMap from file ─────────────────────────────
echo "==> Creating Keycloak realm ConfigMap..."
kubectl create configmap keycloak-realm \
  --from-file=airport-realm.json="$PROJECT_ROOT/keycloak/airport-realm.json" \
  -n airport \
  --dry-run=client -o yaml | kubectl apply -f -

# ── 7. Deploy airport namespace ──────────────────────────────────────────────
echo "==> Deploying airport namespace resources..."
if [ ! -f "$SCRIPT_DIR/secrets.yaml" ]; then
  echo "ERROR: k8s/secrets.yaml not found."
  echo "       Copy k8s/secrets.yaml.example, fill in real values, then re-run."
  exit 1
fi
kubectl apply -f "$SCRIPT_DIR/secrets.yaml"
kubectl apply -f "$SCRIPT_DIR/configmap.yaml"
kubectl apply -f "$SCRIPT_DIR/network-policies/airport.yaml"
kubectl apply -f "$SCRIPT_DIR/mysql/"
kubectl apply -f "$SCRIPT_DIR/rabbitmq/"
kubectl apply -f "$SCRIPT_DIR/keycloak/"
kubectl apply -f "$SCRIPT_DIR/flight/"
kubectl apply -f "$SCRIPT_DIR/booking/"
kubectl apply -f "$SCRIPT_DIR/notification/"
kubectl apply -f "$SCRIPT_DIR/payment/"
kubectl apply -f "$SCRIPT_DIR/ollama/"
kubectl apply -f "$SCRIPT_DIR/assistant/"
kubectl apply -f "$SCRIPT_DIR/gateway/"
kubectl apply -f "$SCRIPT_DIR/baggage/"
kubectl apply -f "$SCRIPT_DIR/monitoring/airport-dashboard.yaml"

# ── 8. Print access URLs ─────────────────────────────────────────────────────
MINIKUBE_IP=$(minikube ip)
echo ""
echo "==> All resources applied."
echo ""
echo "    ── Via gateway (port 30500) ──────────────────────────────────────────"
echo "    GraphQL playground:   http://$MINIKUBE_IP:30500/graphql"
echo "    Booking API:          http://$MINIKUBE_IP:30500/api/Booking/"
echo "    Payment API:          http://$MINIKUBE_IP:30500/api/payment/"
echo "    Assistant health:     http://$MINIKUBE_IP:30500/assistant/health"
echo "    Assistant chat:       http://$MINIKUBE_IP:30500/assistant/chat  (POST)"
echo ""
echo "    ── Direct NodePorts ──────────────────────────────────────────────────"
echo "    Baggage API:          http://$MINIKUBE_IP:30501"
echo "    Keycloak admin:       http://$MINIKUBE_IP:30880  (admin / admin)"
echo "    RabbitMQ management:  http://$MINIKUBE_IP:30672  (guest / guest)"
echo "    Grafana:              http://$MINIKUBE_IP:30300  (admin / admin)"
echo ""
echo "==> Watch rollout:"
echo "    kubectl get pods -n airport -w"
