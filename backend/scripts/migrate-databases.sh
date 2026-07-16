#!/bin/sh
set -e

HOST="${DB_HOST:-postgres}"
PORT="${DB_PORT:-5432}"
USER="${DB_USER:-postgres}"
PASS="${DB_PASSWORD:-postgres}"

run_migrate() {
  db="$1"
  echo "==> Migrating database: $db"
  ConnectionStrings__DefaultConnection="Host=${HOST};Port=${PORT};Database=${db};Username=${USER};Password=${PASS}" \
    dotnet ef database update \
      --project Novacart.Core/Novacart.Core.csproj \
      --context AppDbContext \
      --verbose
}

run_migrate novacart_auth
run_migrate novacart_product
run_migrate novacart_commerce
run_migrate novacart_commerce_0
run_migrate novacart_commerce_1
run_migrate novacart_cart

echo "All service databases migrated."
