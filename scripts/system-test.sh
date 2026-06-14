#!/usr/bin/env bash
# System-level cooperation test
#
# Verifies: BookingService listens on booking_queue and updates booking status
# to Confirmed when it receives PaymentStatusMessage { PaymentSucceeded: true }.
#
# Scope note: PaymentService does not yet publish back to booking_queue, so this
# test drives the consumer directly. Expand once the Payment→Booking reply is
# implemented.
#
# Required tools on the runner: curl, jq, docker
#
# Environment variables (with defaults for local use):
#   BOOKING_HOST   – host:port of BookingService  (default: localhost:5001)
#   RABBITMQ_MGMT  – host:port of RabbitMQ mgmt   (default: localhost:15672)
#   COMPOSE_FILE   – path to system-test compose   (default: docker-compose.system-test.yml)

set -euo pipefail

BOOKING_HOST="${BOOKING_HOST:-localhost:5001}"
RABBITMQ_MGMT="${RABBITMQ_MGMT:-localhost:15672}"
COMPOSE_FILE="${COMPOSE_FILE:-docker-compose.system-test.yml}"

# ── helpers ───────────────────────────────────────────────────────────────────

wait_for_url() {
  local label="$1"
  local url="$2"
  local max="${3:-60}"
  local deadline=$(( SECONDS + max ))
  echo "Waiting for $label..."
  until curl -sf "$url" > /dev/null 2>&1; do
    if [ "$SECONDS" -ge "$deadline" ]; then
      echo "ERROR: timed out waiting for $label ($url)"
      exit 1
    fi
    sleep 3
  done
  echo "  $label is ready"
}

# ── wait for infrastructure ───────────────────────────────────────────────────

wait_for_url "RabbitMQ management API" \
  "http://guest:guest@${RABBITMQ_MGMT}/api/overview" 90

# BookingService exposes Prometheus metrics at /metrics (no auth required)
wait_for_url "BookingService" \
  "http://${BOOKING_HOST}/metrics" 120

# ── seed a test booking directly into the database ───────────────────────────

BOOKING_ID=$(python3 -c "import uuid; print(str(uuid.uuid4()))")
echo ""
echo "=== System test ==="
echo "Booking ID: $BOOKING_ID"

echo "Inserting test booking into PostgreSQL..."
docker compose -f "$COMPOSE_FILE" exec -T booking-db \
  psql -U sysuser -d SystemTestDb -c "
INSERT INTO \"Bookings\" (
  \"Id\", \"UserId\", \"FlightId\", \"IsOneWay\",
  \"TotalPrice\", \"Status\", \"ContactEmail\", \"ContactPhone\", \"CreatedAt\"
) VALUES (
  '$BOOKING_ID'::uuid,
  'ci-test-user',
  'FL-CI-001',
  true,
  999.00,
  0,
  'ci@test.com',
  '00000000',
  NOW()
);"

# ── publish PaymentStatusMessage to booking_queue ─────────────────────────────
# BookingEventConsumer is listening on this queue and will update the status.

echo "Publishing PaymentStatusMessage { BookingId, PaymentSucceeded: true }..."
PAYLOAD=$(printf '{"BookingId":"%s","PaymentSucceeded":true}' "$BOOKING_ID")

PUBLISH_RESULT=$(curl -sf \
  -u "guest:guest" \
  -H "Content-Type: application/json" \
  -d "$(jq -n \
        --arg rk  "booking_queue" \
        --arg p   "$PAYLOAD" \
        '{routing_key:$rk, payload:$p, payload_encoding:"string",
          properties:{delivery_mode:2}}')" \
  "http://${RABBITMQ_MGMT}/api/exchanges/%2F/amq.default/publish")

# RabbitMQ returns {"routed":true} when at least one queue received the message
if ! echo "$PUBLISH_RESULT" | jq -e '.routed == true' > /dev/null 2>&1; then
  echo "WARNING: RabbitMQ reported message was not routed: $PUBLISH_RESULT"
  echo "         booking_queue may not be declared yet — waiting a moment and retrying..."
  sleep 5
  PUBLISH_RESULT=$(curl -sf \
    -u "guest:guest" \
    -H "Content-Type: application/json" \
    -d "$(jq -n \
          --arg rk "booking_queue" \
          --arg p  "$PAYLOAD" \
          '{routing_key:$rk, payload:$p, payload_encoding:"string",
            properties:{delivery_mode:2}}')" \
    "http://${RABBITMQ_MGMT}/api/exchanges/%2F/amq.default/publish")
fi

echo "  Publish result: $PUBLISH_RESULT"

# ── poll until status == Confirmed (2) ────────────────────────────────────────
# BookingStatus enum: Pending=0, AwaitingPayment=1, Confirmed=2, Cancelled=3

echo "Polling GET /api/booking/$BOOKING_ID (expecting status=2)..."
DEADLINE=$(( SECONDS + 30 ))
while [ "$SECONDS" -lt "$DEADLINE" ]; do
  HTTP_CODE=$(curl -o /tmp/booking_result.json \
                   -w "%{http_code}" \
                   -sf \
                   "http://${BOOKING_HOST}/api/booking/$BOOKING_ID" 2>/dev/null \
              || echo "000")

  if [ "$HTTP_CODE" = "200" ]; then
    STATUS=$(jq '.status // -1' /tmp/booking_result.json 2>/dev/null || echo "-1")
    echo "  HTTP 200 | status=$STATUS"
    if [ "$STATUS" = "2" ]; then
      echo ""
      echo "SUCCESS: booking $BOOKING_ID reached Confirmed status (status=2)"
      exit 0
    fi
  else
    echo "  HTTP $HTTP_CODE — retrying..."
  fi
  sleep 2
done

echo ""
echo "FAIL: booking $BOOKING_ID did not reach Confirmed within 30 s"
echo "Final response:"
cat /tmp/booking_result.json 2>/dev/null || true
exit 1
