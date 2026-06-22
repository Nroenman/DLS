#!/usr/bin/env bash
# Publish a burst of messages to the Notification queue to demo KEDA autoscaling.
# KEDA scales the notification deployment 0 -> 5 based on this queue's depth.
#
# Usage: bash k8s/demo-load.sh [count]    (default 500)
set -euo pipefail

COUNT="${1:-500}"
echo "Publishing ${COUNT} messages to the 'Notification' queue..."

kubectl run "pub-$RANDOM" --rm -i --restart=Never -n airport \
  --image=curlimages/curl:latest -- \
  sh -c "for i in \$(seq 1 ${COUNT}); do curl -s -u guest:guest -H 'content-type:application/json' -d '{\"properties\":{},\"routing_key\":\"Notification\",\"payload\":\"demo\",\"payload_encoding\":\"string\"}' http://rabbitmq:15672/api/exchanges/%2F/amq.default/publish >/dev/null; done; echo published ${COUNT}"
