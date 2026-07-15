#!/usr/bin/env sh
# PE-1 microservices smoke + checkout-path verification (run after: docker compose up -d)
set -e

GATEWAY="${GATEWAY_URL:-http://localhost:5000}"
MAX_WAIT="${MAX_WAIT_SECONDS:-120}"

echo "==> Waiting for gateway at $GATEWAY (max ${MAX_WAIT}s)..."

elapsed=0
until curl -sf "$GATEWAY/api/health" >/dev/null 2>&1; do
  sleep 3
  elapsed=$((elapsed + 3))
  if [ "$elapsed" -ge "$MAX_WAIT" ]; then
    echo "ERROR: gateway not healthy after ${MAX_WAIT}s"
    exit 1
  fi
done

echo "==> Gateway health OK"

echo "==> GET /api/products"
curl -sf "$GATEWAY/api/products?page=1&pageSize=1" | grep -q '"items"' \
  || { echo "ERROR: products list failed"; exit 1; }

echo "==> GET /api/currency (product cluster)"
curl -sf "$GATEWAY/api/currency" >/dev/null \
  || echo "WARN: currency endpoint unavailable (non-fatal)"

echo "==> RabbitMQ management (optional)"
curl -sf -u guest:guest http://localhost:15672/api/overview >/dev/null 2>&1 \
  && echo "RabbitMQ management OK" \
  || echo "WARN: RabbitMQ management not reachable on :15672"

echo "==> Jaeger UI (optional)"
curl -sf http://localhost:16686 >/dev/null 2>&1 \
  && echo "Jaeger UI OK" \
  || echo "WARN: Jaeger not reachable on :16686"

echo ""
echo "PE-1 microservices smoke checks passed."
echo "For full checkout: register → add to cart → Stripe test webhook (see docs/STRIPE_WEBHOOK_LOCAL.md)."
