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
kubectl apply -f "$SCRIPT_DIR/secrets.yaml"
kubectl apply -f "$SCRIPT_DIR/configmap.yaml"
kubectl apply -f "$SCRIPT_DIR/postgres/"
kubectl apply -f "$SCRIPT_DIR/rabbitmq/"
kubectl apply -f "$SCRIPT_DIR/keycloak/"
kubectl apply -f "$SCRIPT_DIR/flight/"
kubectl apply -f "$SCRIPT_DIR/booking/"
kubectl apply -f "$SCRIPT_DIR/notification/"
kubectl apply -f "$SCRIPT_DIR/gateway/"

# ── 8. Deploy baggage namespace ──────────────────────────────────────────────
echo "==> Deploying baggage namespace resources..."
kubectl apply -f "$SCRIPT_DIR/baggage/"

# ── 9. Print access URLs ─────────────────────────────────────────────────────
MINIKUBE_IP=$(minikube ip)
echo ""
echo "==> All resources applied. Access points:"
echo "    Gateway (API):        http://$MINIKUBE_IP:30500"
echo "    Keycloak admin:       http://$MINIKUBE_IP:30880  (admin / admin)"
echo "    RabbitMQ management:  http://$MINIKUBE_IP:30672  (guest / guest)"
echo "    Baggage API:          http://$MINIKUBE_IP:30501"
echo ""
echo "==> Watch rollout:"
echo "    kubectl get pods -n airport -w"
echo "    kubectl get pods -n baggage -w"
