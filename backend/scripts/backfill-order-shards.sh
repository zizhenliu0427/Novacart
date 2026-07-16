#!/bin/sh
set -e

# PE-7: copy legacy novacart_commerce orders into UserId-routed shards.
# Default is dry-run. Pass --apply to write; optional --delete-legacy after verify.
#
# Required env (same as order-api):
#   ConnectionStrings__DefaultConnection  -> novacart_commerce (routing + legacy orders)
#   ConnectionStrings__CommerceShard0
#   ConnectionStrings__CommerceShard1
#   OrderSharding__Enabled=true
#   OrderSharding__ShardCount=2

SCRIPT_DIR="$(CDPATH= cd -- "$(dirname "$0")" && pwd)"
BACKEND_DIR="$(CDPATH= cd -- "$SCRIPT_DIR/.." && pwd)"

cd "$BACKEND_DIR"

echo "==> Order shard backfill (dry-run unless --apply passed through)"
dotnet run --project scripts/OrderShardBackfill/OrderShardBackfill.csproj -- "$@"
