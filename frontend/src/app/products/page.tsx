'use client';

import { useCallback, useEffect, useState } from 'react';
import { Card } from '@/components/ui/Card';
import { EmptyState } from '@/components/ui/EmptyState';
import { ProductCard } from '@/components/ProductCard';
import { Input } from '@/components/ui/Input';
import { GridIcon } from '@/components/icons';
import type { PagedResult, Product } from '@/types/product';

const SORT_OPTIONS = [
  { value: 'newest', label: 'Newest' },
  { value: 'price_asc', label: 'Price: Low → High' },
  { value: 'price_desc', label: 'Price: High → Low' },
  { value: 'name_asc', label: 'Name (A–Z)' },
];

const CATEGORIES = [
  { id: 1, name: 'Electronics' },
  { id: 2, name: 'Apparel' },
  { id: 3, name: 'Home & Living' },
  { id: 4, name: 'Accessories' },
  { id: 5, name: 'Books' },
];

const POPULAR_TAGS = [
  'bestseller',
  'wireless',
  'gaming',
  'sustainable',
  'compact',
  'organic',
];

export default function ProductsPage() {
  const [products, setProducts] = useState<Product[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Search & sorting
  const [query, setQuery] = useState('');
  const [debouncedQuery, setDebouncedQuery] = useState('');
  const [sort, setSort] = useState('newest');

  // Filters
  const [selectedCategories, setSelectedCategories] = useState<number[]>([]);
  const [minPrice, setMinPrice] = useState('');
  const [maxPrice, setMaxPrice] = useState('');
  const [appliedMinPrice, setAppliedMinPrice] = useState<string>('');
  const [appliedMaxPrice, setAppliedMaxPrice] = useState<string>('');
  const [selectedTag, setSelectedTag] = useState<string | null>(null);

  const [mobileFiltersOpen, setMobileFiltersOpen] = useState(false);

  // Debounce search query
  useEffect(() => {
    const t = setTimeout(() => setDebouncedQuery(query), 350);
    return () => clearTimeout(t);
  }, [query]);

  const fetchProducts = useCallback(async () => {
    setLoading(true);
    setError(null);
    const params = new URLSearchParams({ sort, page: '1', pageSize: '50' });
    
    if (debouncedQuery) params.set('q', debouncedQuery);
    
    // Pass categoryIds as multiple params or single comma-separated if supported (backend supports int[] from query)
    if (selectedCategories.length > 0) {
      selectedCategories.forEach((id) => params.append('categoryIds', id.toString()));
    }
    
    if (appliedMinPrice) params.set('minPrice', appliedMinPrice);
    if (appliedMaxPrice) params.set('maxPrice', appliedMaxPrice);
    if (selectedTag) params.set('tag', selectedTag);

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
  }, [debouncedQuery, sort, selectedCategories, appliedMinPrice, appliedMaxPrice, selectedTag]);

  useEffect(() => {
    fetchProducts();
  }, [fetchProducts]);

  const toggleCategory = (id: number) => {
    setSelectedCategories((prev) =>
      prev.includes(id) ? prev.filter((x) => x !== id) : [...prev, id]
    );
  };

  const handleApplyPrice = (e: React.FormEvent) => {
    e.preventDefault();
    setAppliedMinPrice(minPrice);
    setAppliedMaxPrice(maxPrice);
  };

  const handleClearFilters = () => {
    setSelectedCategories([]);
    setMinPrice('');
    setMaxPrice('');
    setAppliedMinPrice('');
    setAppliedMaxPrice('');
    setSelectedTag(null);
  };

  const filterSidebar = (
    <div className="space-y-6">
      <div>
        <div className="flex items-center justify-between">
          <h2 className="text-sm font-semibold uppercase tracking-wider text-ink-muted">Filters</h2>
          <button
            onClick={handleClearFilters}
            className="text-xs text-accent hover:underline"
          >
            Clear all
          </button>
        </div>
      </div>

      {/* Categories */}
      <div className="space-y-2">
        <h3 className="text-sm font-semibold text-ink">Categories</h3>
        <div className="flex flex-col gap-2">
          {CATEGORIES.map((cat) => {
            const checked = selectedCategories.includes(cat.id);
            return (
              <label key={cat.id} className="flex items-center gap-2 text-sm text-ink-muted hover:text-ink cursor-pointer">
                <input
                  type="checkbox"
                  checked={checked}
                  onChange={() => toggleCategory(cat.id)}
                  className="rounded border-border text-accent focus:ring-accent"
                />
                {cat.name}
              </label>
            );
          })}
        </div>
      </div>

      {/* Price filter */}
      <div className="space-y-2">
        <h3 className="text-sm font-semibold text-ink">Price Range</h3>
        <form onSubmit={handleApplyPrice} className="flex gap-2">
          <Input
            type="number"
            placeholder="Min"
            value={minPrice}
            onChange={(e) => setMinPrice(e.target.value)}
            className="h-9 text-xs"
          />
          <Input
            type="number"
            placeholder="Max"
            value={maxPrice}
            onChange={(e) => setMaxPrice(e.target.value)}
            className="h-9 text-xs"
          />
          <button
            type="submit"
            className="rounded-lg bg-accent px-3 py-1.5 text-xs font-medium text-accent-contrast transition hover:bg-accent-hover"
          >
            Apply
          </button>
        </form>
      </div>

      {/* Tags */}
      <div className="space-y-2">
        <h3 className="text-sm font-semibold text-ink">Popular Tags</h3>
        <div className="flex flex-wrap gap-1.5">
          {POPULAR_TAGS.map((tag) => {
            const active = selectedTag === tag;
            return (
              <button
                key={tag}
                onClick={() => setSelectedTag(active ? null : tag)}
                className={`rounded-full px-2.5 py-1 text-xs transition ${
                  active
                    ? 'bg-accent text-accent-contrast font-medium'
                    : 'bg-bg-subtle text-ink-muted hover:bg-border hover:text-ink'
                }`}
              >
                #{tag}
              </button>
            );
          })}
        </div>
      </div>
    </div>
  );

  return (
    <div className="space-y-6">
      {/* Header + Search bar */}
      <div className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight text-ink">Products</h1>
          <p className="text-sm text-ink-muted">
            {loading
              ? 'Loading catalogue…'
              : `${products.length}${totalCount > products.length ? ` of ${totalCount}` : ''} item${products.length === 1 ? '' : 's'}`}
          </p>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          {/* Mobile filter toggle */}
          <button
            onClick={() => setMobileFiltersOpen((o) => !o)}
            className="h-10 rounded-lg border border-border bg-surface px-3 text-sm text-ink hover:bg-bg-subtle lg:hidden"
          >
            Filters
          </button>
          <Input
            id="product-search"
            type="search"
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            placeholder="Search products…"
            className="max-w-xs"
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

      {/* Main layout with Filters sidebar */}
      <div className="grid gap-8 lg:grid-cols-[240px_1fr]">
        {/* Desktop Sidebar */}
        <aside className="hidden lg:block border-r border-border pr-6">
          {filterSidebar}
        </aside>

        {/* Mobile Sidebar overlay */}
        {mobileFiltersOpen && (
          <div className="rounded-xl border border-border bg-surface p-4 lg:hidden">
            {filterSidebar}
          </div>
        )}

        {/* Content area */}
        <div className="space-y-6">
          {/* Active filter summary (tags / min/max / categories) */}
          {(selectedCategories.length > 0 || appliedMinPrice || appliedMaxPrice || selectedTag) && (
            <div className="flex flex-wrap items-center gap-2 text-xs">
              <span className="text-ink-muted">Active filters:</span>
              {selectedCategories.map((id) => (
                <span key={id} className="rounded-full bg-accent-weak px-2 py-0.5 text-accent">
                  {CATEGORIES.find((c) => c.id === id)?.name}
                </span>
              ))}
              {(appliedMinPrice || appliedMaxPrice) && (
                <span className="rounded-full bg-accent-weak px-2 py-0.5 text-accent">
                  Price: ${appliedMinPrice || '0'} - ${appliedMaxPrice || '∞'}
                </span>
              )}
              {selectedTag && (
                <span className="rounded-full bg-accent-weak px-2 py-0.5 text-accent">
                  #{selectedTag}
                </span>
              )}
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
          {!loading && !error && products.length > 0 && (
            <div className="grid grid-cols-1 gap-5 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
              {products.map((product) => (
                <ProductCard key={product.id} product={product} />
              ))}
            </div>
          )}

          {!loading && !error && products.length === 0 && (
            <EmptyState
              icon={<GridIcon />}
              title="No products match"
              description="Try a different search term, category, or filter criteria."
            />
          )}
        </div>
      </div>
    </div>
  );
}
