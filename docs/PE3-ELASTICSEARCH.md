# PE-3 — ElasticSearch Product Search

> **Status:** Implemented on **Product API** (2026-07-16). Keyword queries (`?q=`) use Elasticsearch when enabled and healthy; otherwise the existing Postgres `ILIKE` path runs unchanged.

## Stack

| Component | Choice |
|-----------|--------|
| Search engine | Elasticsearch **8.15** (single-node dev; managed cluster in prod) |
| .NET client | `Elastic.Clients.Elasticsearch` 8.15 |
| Index | `novacart-products` (configurable via `Elasticsearch:IndexName`) |
| Service | Product API only — `/api/products` contract unchanged |

## Configuration

```json
"Elasticsearch": {
  "Enabled": true,
  "Url": "http://elasticsearch:9200",
  "IndexName": "novacart-products",
  "ReindexOnStartup": true
}
```

Docker Compose enables ES by default for `product-api`. Local `dotnet run` without ES keeps `Enabled: false`.

## Index sync

| Event | Action |
|-------|--------|
| Product API startup | Ensure index + reindex all active products (when `ReindexOnStartup`) |
| Admin create/update | Index document |
| Admin deactivate | Remove document |
| Square catalogue sync | Full reindex |

Documents include: name, description, tags, flattened metadata text, category name, price, filters.

## Search behaviour

- **Keyword present (`q`)** + ES healthy → multi-match on name (boosted), description, tags, metadata, category; filters for category/price/tag; sort by relevance or explicit sort param.
- **ES down or error** → automatic fallback to Postgres ILIKE; response `searchEngine: "postgres"`.
- **Browse (no `q`)** → Postgres only (facets/sort unchanged).

API adds optional `searchEngine` on `PagedResult`: `"elasticsearch"` | `"postgres"`.

## Benchmark notes

On seeded catalogue (~12 products), Postgres ILIKE and Elasticsearch are both sub-10 ms in-process — ES advantage appears at scale (10k+ SKUs, fuzzy/stemming, metadata relevance). Integration tests in `ProductSearchIntegrationTests` validate ES path + fallback; run with Docker:

```bash
dotnet test backend.Tests/Novacart.Api.Tests.csproj --filter ProductSearch
```

Production: point `ELASTICSEARCH_URL` at your managed cluster (see `.env.prod.example`).
