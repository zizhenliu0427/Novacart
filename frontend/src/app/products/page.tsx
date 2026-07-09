'use client';

import { useCallback, useEffect, useMemo, useState } from 'react';
import { ProductCard } from '@/components/ProductCard';
import { Card } from '@/components/ui/Card';
import { EmptyState } from '@/components/ui/EmptyState';
import { GridIcon } from '@/components/icons';
import type { Product, PagedResult } from '@/types/product';

const SORT_OPTIONS = [
  { value: 'newest', label: 'Newest' },
  { value: 'price_asc', label: 'Price: Low → High' },
  { value: 'price_desc', label: 'Price: High → Low' },
  { value: 'name_asc', label: 'Name (A–Z)' },
];

export default function ProductsPage() {
  const [products, setProducts] = useState<Product[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [query, setQuery] = useState('');
  const [debouncedQuery, setDebouncedQuery] = useState('');
  const [category, setCategory] = useState('All');
  const [sort, setSort] = useState('newest');

  // Debounce the search query
  useEffect(() => {
    const t = setTimeout(() => setDebouncedQuery(query), 350);
    return () => clearTimeout(t);
  }, [query]);

  const fetchProducts = useCallback(async () => {
    setLoading(true);
    setError(null);
    const params = new URLSearchParams({ sort, page: '1', pageSize: '50' });
    if (debouncedQuery) params.set('q', debouncedQuery);
    try {
      const res = await fetch(`/api/products?${params}`);
      if (!res.ok) throw new Error(`Request failed (${res.status})`);
      const data: PagedResult<Product> = await res.json();
      setProducts(data.items);
      setTotalCount(data.totalCount);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load products');
    } finally {
      setLoading(false);
    }
  }, [debouncedQuery, sort]);

  useEffect(() => { fetchProducts(); }, [fetchProducts]);

  // Derive categories from loaded products (client-side filter for the chip bar)
  const categories = useMemo(
    () => ['All', ...Array.from(new Set(products.map((p) => p.categoryName ?? '')))
      .filter(Boolean).sort()],
    [products],
  );

  // Apply category chip filter client-side (server handles search + sort)
  const visible = useMemo(
    () =>
      category === 'All'
        ? products
        : products.filter((p) => p.categoryName === category),
    [products, category],
  );

  return (
    <div className="space-y-6">
      {/* Header + search */}
      <div className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight text-ink">Products</h1>
          <p className="text-sm text-ink-muted">
            {loading
              ? 'Loading catalogue…'
              : `${visible.length}${totalCount > visible.length ? ` of ${totalCount}` : ''} item${visible.length === 1 ? '' : 's'}`}
          </p>
        </div>
        <div className="flex items-center gap-2">
          <input
            id="product-search"
            type="search"
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            placeholder="Search products…"
            className="h-10 w-full max-w-xs rounded-lg border border-border bg-surface px-3 text-sm text-ink placeholder:text-ink-muted focus:border-accent focus:outline-none focus:ring-2 focus:ring-accent/20"
          />
          <select
            id="product-sort"
            value={sort}
            onChange={(e) => setSort(e.target.value)}
            className="h-10 rounded-lg border border-border bg-surface px-3 text-sm text-ink focus:border-accent focus:outline-none"
          >
            {SORT_OPTIONS.map((o) => (
              <option key={o.value} value={o.value}>{o.label}</option>
            ))}
          </select>
        </div>
      </div>

      {/* Category chips */}
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

      {/* Error */}
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
          description="Try a different search term, category, or sort order."
        />
      )}
    </div>
  );
}
