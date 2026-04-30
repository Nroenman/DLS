#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

if ! command -v kubectl &>/dev/null; then
  kubectl() { minikube kubectl -- "$@"; }
fi

echo "==> Adding Helm repos..."
helm repo add prometheus-community https://prometheus-community.github.io/helm-charts
helm repo add grafana https://grafana.github.io/helm-charts
helm repo update

# ── Prometheus + Grafana + node-exporter + kube-state-metrics ───────────────
echo "==> Installing kube-prometheus-stack..."
helm upgrade --install monitoring prometheus-community/kube-prometheus-stack \
  --namespace monitoring \
  --create-namespace \
  --values "$SCRIPT_DIR/prometheus-stack-values.yaml" \
  --wait --timeout 5m

# ── Loki + Promtail ──────────────────────────────────────────────────────────
echo "==> Installing loki-stack..."
helm upgrade --install loki grafana/loki-stack \
  --namespace monitoring \
  --values "$SCRIPT_DIR/loki-values.yaml" \
  --wait --timeout 3m

# ── Postgres exporter + ServiceMonitors ─────────────────────────────────────
echo "==> Applying postgres exporter and service monitors..."
kubectl apply -f "$SCRIPT_DIR/postgres-exporter.yaml"
kubectl apply -f "$SCRIPT_DIR/service-monitors.yaml"

MINIKUBE_IP=$(minikube ip)
echo ""
echo "==> Monitoring stack ready."
echo "    Grafana: http://$MINIKUBE_IP:30300  (admin / admin)"
echo ""
echo "    Pre-loaded dashboards (Airport System folder):"
echo "      - Node Exporter Full"
echo "      - Kubernetes Cluster"
echo "      - RabbitMQ Overview"
echo "      - PostgreSQL Database"
echo "      - ASP.NET Core (active once services expose /metrics)"
echo ""
echo "    Logs: open Explore → select Loki datasource → filter by {namespace=\"airport\"}"
