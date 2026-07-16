#!/bin/sh
set -e

# PE-8: profile checkout + webhook thread-pool pressure with dotnet-counters.
# Usage:
#   ./scripts/profile-threadpool.sh                    # list dotnet processes
#   ./scripts/profile-threadpool.sh order-api          # monitor order-api by name fragment
#
# Requires .NET SDK on the host (or run inside Dockerfile.backend.test).

PROCESS="${1:-order-api}"

if ! command -v dotnet-counters >/dev/null 2>&1; then
  echo "Installing dotnet-counters tool..."
  dotnet tool install --global dotnet-counters
fi

echo "==> Matching processes:"
dotnet-counters ps | grep -i "$PROCESS" || true

PID="$(dotnet-counters ps | grep -i "$PROCESS" | head -n1 | awk '{print $1}')"
if [ -z "$PID" ]; then
  echo "No process matched '$PROCESS'. Start the stack first (docker compose up)."
  exit 1
fi

echo "==> Monitoring PID $PID (Ctrl+C to stop)"
dotnet-counters monitor --process-id "$PID" \
  --counters System.Runtime[threadpool-thread-count,threadpool-queue-length,cpu-usage,gc-heap-size] \
  --counters Novacart.Runtime \
  --counters Novacart.Webhook
