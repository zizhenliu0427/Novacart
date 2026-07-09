'use client';

import { useEffect, useMemo, useState } from 'react';
import { ProductCard } from '@/components/ProductCard';
import { Card } from '@/components/ui/Card';
import { EmptyState } from '@/components/ui/EmptyState';
import { GridIcon } from '@/components/icons';
import type { Product } from '@/types/product';

export default function ProductsPage() {
  const [products, setProducts] = useState<Product[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [query, setQuery] = useState('');
  const [category, setCategory] = useState('All');

  useEffect(() => {
    fetch('/api/products')
      .then((res) => {
        if (!res.ok) throw new Error(`Request failed (${res.status})`);
        return res.json();
      })
      .then(setProducts)
      .catch((err) => setError(err.message))
      .finally(() => setLoading(false));
  }, []);

  const categories = useMemo(
    () => ['All', ...Array.from(new Set(products.map((p) => p.category)))],
    [products],
  );

  const visible = useMemo(
    () =>
      products.filter(
        (p) =>
          (category === 'All' || p.category === category) &&
          (query === '' ||
            `${p.name} ${p.description}`.toLowerCase().includes(query.toLowerCase())),
      ),
    [products, category, query],
  );

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight text-ink">Products</h1>
          <p className="text-sm text-ink-muted">
            {loading ? 'Loading catalogue…' : `${visible.length} item${visible.length === 1 ? '' : 's'}`}
          </p>
        </div>
        <input
          type="search"
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          placeholder="Search products…"
          className="h-10 w-full max-w-xs rounded-lg border border-border bg-surface px-3 text-sm text-ink placeholder:text-ink-muted focus:border-accent"
        />
      </div>

      {/* Category filter chips */}
      {!loading && !error && categories.length > 1 && (
        <div className="flex flex-wrap gap-2">
          {categories.map((c) => (
            <button
              key={c}
              onClick={() => setCategory(c)}
              className={`rounded-full border px-3 py-1.5 text-sm transition ${
                category === c
                  ? 'border-transparent bg-accent-weak text-accent'
                  : 'border-border text-ink-muted hover:bg-bg-subtle hover:text-ink'
              }`}
            >
              {c}
            </button>
          ))}
        </div>
      )}

      {error && (
        <EmptyState
          icon={<GridIcon />}
          title="Couldn't load products"
          description={error}
        />
      )}

      {/* Loading skeletons */}
      {loading && (
        <div className="grid grid-cols-1 gap-5 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
          {Array.from({ length: 8 }).map((_, i) => (
            <Card key={i} className="overflow-hidden">
              <div className="aspect-square animate-pulse bg-bg-subtle" />
              <div className="space-y-3 p-4">
                <div className="h-3 w-16 animate-pulse rounded bg-bg-subtle" />
                <div className="h-4 w-3/4 animate-pulse rounded bg-bg-subtle" />
                <div className="h-8 w-full animate-pulse rounded bg-bg-subtle" />
              </div>
            </Card>
          ))}
        </div>
      )}

      {/* Grid */}
      {!loading && !error && visible.length > 0 && (
        <div className="grid grid-cols-1 gap-5 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
          {visible.map((product) => (
            <ProductCard key={product.id} product={product} />
          ))}
        </div>
      )}

      {!loading && !error && visible.length === 0 && (
        <EmptyState
          icon={<GridIcon />}
          title="No products match"
          description="Try a different search term or category."
        />
      )}
    </div>
  );
}
