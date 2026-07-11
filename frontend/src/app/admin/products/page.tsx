'use client';

import { FormEvent, useCallback, useEffect, useState } from 'react';
import { Badge } from '@/components/ui/Badge';
import { Button } from '@/components/ui/Button';
import { Card } from '@/components/ui/Card';
import { EmptyState } from '@/components/ui/EmptyState';
import { Input } from '@/components/ui/Input';
import { GridIcon } from '@/components/icons';
import { useAuth } from '@/contexts/AuthContext';
import { apiCall } from '@/lib/api';
import type {
  AdminProduct,
  AdminProductRequest,
  CategoryOption,
  PagedResult,
} from '@/types/product';
import { formatPrice } from '@/types/product';

type StatusFilter = 'all' | 'active' | 'inactive';

interface ProductFormState {
  name: string;
  slug: string;
  description: string;
  price: string;
  currency: string;
  stockQuantity: string;
  categoryId: string;
  tags: string;
  metadata: string;
  isActive: boolean;
}

const EMPTY_FORM: ProductFormState = {
  name: '',
  slug: '',
  description: '',
  price: '',
  currency: 'AUD',
  stockQuantity: '0',
  categoryId: '',
  tags: '',
  metadata: '',
  isActive: true,
};

function slugify(value: string): string {
  return value
    .toLowerCase()
    .trim()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-|-$/g, '');
}

function toForm(product: AdminProduct): ProductFormState {
  return {
    name: product.name,
    slug: product.slug,
    description: product.description ?? '',
    price: String(product.price),
    currency: product.currency,
    stockQuantity: String(product.stockQuantity),
    categoryId: product.categoryId ? String(product.categoryId) : '',
    tags: product.tags.join(', '),
    metadata: product.metadata ?? '',
    isActive: product.isActive,
  };
}

export default function AdminProductsPage() {
  const { user } = useAuth();
  const [products, setProducts] = useState<AdminProduct[]>([]);
  const [categories, setCategories] = useState<CategoryOption[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(0);
  const [page, setPage] = useState(1);
  const [query, setQuery] = useState('');
  const [debouncedQuery, setDebouncedQuery] = useState('');
  const [status, setStatus] = useState<StatusFilter>('all');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);

  const [formOpen, setFormOpen] = useState(false);
  const [editing, setEditing] = useState<AdminProduct | null>(null);
  const [form, setForm] = useState<ProductFormState>(EMPTY_FORM);
  const [formError, setFormError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [syncing, setSyncing] = useState(false);

  async function triggerSquareSync() {
    if (!user) return;
    setSyncing(true);
    setError(null);
    setNotice(null);
    try {
      const res = await apiCall<{ success: boolean; message: string }>('/admin/products/sync-square', {
        method: 'POST',
      });
      setNotice(res.message);
      await loadProducts();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to sync with Square.');
    } finally {
      setSyncing(false);
    }
  }

  useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedQuery(query.trim());
      setPage(1);
    }, 300);
    return () => clearTimeout(timer);
  }, [query]);

  const loadProducts = useCallback(async () => {
    if (!user) return;
    setLoading(true);
    setError(null);
    const params = new URLSearchParams({ page: String(page), pageSize: '20' });
    if (debouncedQuery) params.set('q', debouncedQuery);
    if (status !== 'all') params.set('isActive', String(status === 'active'));

    try {
      const data = await apiCall<PagedResult<AdminProduct>>(`/admin/products?${params}`);
      setProducts(data.items);
      setTotalCount(data.totalCount);
      setTotalPages(data.totalPages);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load products.');
    } finally {
      setLoading(false);
    }
  }, [user, page, debouncedQuery, status]);

  useEffect(() => {
    loadProducts();
  }, [loadProducts]);

  useEffect(() => {
    if (!user) return;
    apiCall<CategoryOption[]>('/admin/products/categories')
      .then(setCategories)
      .catch((err) => setError(err instanceof Error ? err.message : 'Failed to load categories.'));
  }, [user]);

  function openCreate() {
    setEditing(null);
    setForm(EMPTY_FORM);
    setFormError(null);
    setFormOpen(true);
  }

  function openEdit(product: AdminProduct) {
    setEditing(product);
    setForm(toForm(product));
    setFormError(null);
    setFormOpen(true);
  }

  function closeForm() {
    if (submitting) return;
    setFormOpen(false);
    setEditing(null);
    setFormError(null);
  }

  async function submitProduct(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!user) return;
    setFormError(null);

    const price = Number(form.price);
    const stockQuantity = Number(form.stockQuantity);
    if (!Number.isFinite(price) || price <= 0) {
      setFormError('Price must be greater than zero.');
      return;
    }
    if (!Number.isInteger(stockQuantity) || stockQuantity < 0) {
      setFormError('Stock must be a whole number of zero or more.');
      return;
    }
    if (form.metadata.trim()) {
      try {
        const parsed: unknown = JSON.parse(form.metadata);
        if (!parsed || Array.isArray(parsed) || typeof parsed !== 'object') throw new Error();
      } catch {
        setFormError('Metadata must be a valid JSON object.');
        return;
      }
    }

    const request: AdminProductRequest = {
      name: form.name.trim(),
      slug: form.slug.trim(),
      description: form.description.trim() || undefined,
      price,
      currency: form.currency.trim().toUpperCase(),
      stockQuantity,
      categoryId: form.categoryId ? Number(form.categoryId) : undefined,
      tags: form.tags.split(',').map((tag) => tag.trim()).filter(Boolean),
      metadata: form.metadata.trim() || undefined,
      isActive: form.isActive,
    };

    setSubmitting(true);
    try {
      if (editing) {
        await apiCall<AdminProduct>(`/admin/products/${editing.id}`, {
          method: 'PUT', body: request,
        });
        setNotice(`Updated ${request.name}.`);
      } else {
        await apiCall<AdminProduct>('/admin/products', {
          method: 'POST', body: request,
        });
        setNotice(`Created ${request.name}.`);
      }
      setFormOpen(false);
      setEditing(null);
      await loadProducts();
    } catch (err) {
      setFormError(err instanceof Error ? err.message : 'Failed to save product.');
    } finally {
      setSubmitting(false);
    }
  }

  async function deactivate(product: AdminProduct) {
    if (!user || !window.confirm(`Deactivate "${product.name}"? It will disappear from the storefront.`)) return;
    setError(null);
    try {
      await apiCall<void>(`/admin/products/${product.id}`, { method: 'DELETE' });
      setNotice(`Deactivated ${product.name}.`);
      await loadProducts();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to deactivate product.');
    }
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight text-ink">Products & inventory</h1>
          <p className="mt-1 text-sm text-ink-muted">
            {loading ? 'Loading catalogue…' : `${totalCount} product${totalCount === 1 ? '' : 's'}`}
          </p>
        </div>
        <div className="flex gap-2">
          <Button variant="secondary" onClick={triggerSquareSync} disabled={syncing}>
            {syncing ? 'Syncing...' : 'Sync with Square'}
          </Button>
          <Button onClick={openCreate}>Add product</Button>
        </div>
      </div>

      {notice && (
        <div className="flex items-center justify-between rounded-lg border border-border bg-bg-subtle px-4 py-3 text-sm text-success">
          <span>{notice}</span>
          <button onClick={() => setNotice(null)} className="text-ink-muted hover:text-ink" aria-label="Dismiss">×</button>
        </div>
      )}

      <div className="flex flex-wrap gap-3">
        <div className="min-w-64 flex-1">
          <Input
            type="search"
            value={query}
            onChange={(event) => setQuery(event.target.value)}
            placeholder="Search name or slug…"
            aria-label="Search products"
          />
        </div>
        <select
          value={status}
          onChange={(event) => { setStatus(event.target.value as StatusFilter); setPage(1); }}
          className="h-10 rounded-lg border border-border bg-surface px-3 text-sm text-ink focus:border-accent focus:outline-none"
          aria-label="Filter by status"
        >
          <option value="all">All statuses</option>
          <option value="active">Active</option>
          <option value="inactive">Inactive</option>
        </select>
      </div>

      {error && (
        <EmptyState icon={<GridIcon />} title="Couldn't load catalogue" description={error} />
      )}

      {!error && (
        <Card className="overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full min-w-[760px] text-left text-sm">
              <thead className="border-b border-border bg-bg-subtle text-xs uppercase tracking-wide text-ink-muted">
                <tr>
                  <th className="px-4 py-3 font-semibold">Product</th>
                  <th className="px-4 py-3 font-semibold">Category</th>
                  <th className="px-4 py-3 font-semibold">Price</th>
                  <th className="px-4 py-3 font-semibold">Stock</th>
                  <th className="px-4 py-3 font-semibold">Status</th>
                  <th className="px-4 py-3 text-right font-semibold">Actions</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-border">
                {loading && Array.from({ length: 5 }).map((_, index) => (
                  <tr key={index} className="animate-pulse">
                    <td colSpan={6} className="px-4 py-4"><div className="h-5 rounded bg-bg-subtle" /></td>
                  </tr>
                ))}
                {!loading && products.map((product) => (
                  <tr key={product.id} className="hover:bg-bg-subtle/60">
                    <td className="px-4 py-3">
                      <p className="font-medium text-ink">{product.name}</p>
                      <p className="text-xs text-ink-muted">{product.slug}</p>
                    </td>
                    <td className="px-4 py-3 text-ink-muted">{product.categoryName ?? 'Uncategorised'}</td>
                    <td className="px-4 py-3 font-medium text-ink tnum">{formatPrice(product.price, product.currency)}</td>
                    <td className="px-4 py-3">
                      <Badge tone={product.stockQuantity === 0 ? 'danger' : product.stockQuantity <= 10 ? 'warning' : 'neutral'}>
                        {product.stockQuantity === 0 ? 'Out of stock' : product.stockQuantity}
                      </Badge>
                    </td>
                    <td className="px-4 py-3">
                      <Badge tone={product.isActive ? 'success' : 'neutral'}>{product.isActive ? 'Active' : 'Inactive'}</Badge>
                    </td>
                    <td className="px-4 py-3">
                      <div className="flex justify-end gap-2">
                        <Button variant="secondary" size="sm" onClick={() => openEdit(product)}>Edit</Button>
                        {product.isActive && (
                          <Button variant="ghost" size="sm" className="text-danger" onClick={() => deactivate(product)}>
                            Deactivate
                          </Button>
                        )}
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          {!loading && products.length === 0 && (
            <div className="p-8 text-center text-sm text-ink-muted">No products match these filters.</div>
          )}
          {!loading && totalPages > 1 && (
            <div className="flex items-center justify-between border-t border-border px-4 py-3">
              <Button variant="secondary" size="sm" disabled={page <= 1} onClick={() => setPage((value) => value - 1)}>
                Previous
              </Button>
              <span className="text-xs text-ink-muted">Page {page} of {totalPages}</span>
              <Button variant="secondary" size="sm" disabled={page >= totalPages} onClick={() => setPage((value) => value + 1)}>
                Next
              </Button>
            </div>
          )}
        </Card>
      )}

      {formOpen && (
        <div className="fixed inset-0 z-50 flex items-start justify-center overflow-y-auto bg-black/50 p-4 sm:p-8" role="dialog" aria-modal="true" aria-labelledby="product-form-title">
          <Card className="w-full max-w-3xl p-5 sm:p-6">
            <div className="mb-5 flex items-start justify-between gap-4">
              <div>
                <h2 id="product-form-title" className="text-xl font-semibold text-ink">{editing ? 'Edit product' : 'Add product'}</h2>
                <p className="text-sm text-ink-muted">Changes to active products appear in the storefront immediately.</p>
              </div>
              <button onClick={closeForm} className="text-2xl leading-none text-ink-muted hover:text-ink" aria-label="Close">×</button>
            </div>

            <form onSubmit={submitProduct} className="space-y-5">
              <div className="grid gap-4 sm:grid-cols-2">
                <Input
                  label="Name"
                  required
                  minLength={2}
                  maxLength={300}
                  value={form.name}
                  onChange={(event) => setForm((value) => ({ ...value, name: event.target.value, ...(!editing ? { slug: slugify(event.target.value) } : {}) }))}
                />
                <Input
                  label="Slug"
                  required
                  pattern="[a-z0-9]+(?:-[a-z0-9]+)*"
                  value={form.slug}
                  onChange={(event) => setForm((value) => ({ ...value, slug: event.target.value }))}
                  helperText="Lowercase letters, numbers and hyphens."
                />
                <Input
                  label="Price"
                  type="number"
                  required
                  min="0.01"
                  step="0.01"
                  value={form.price}
                  onChange={(event) => setForm((value) => ({ ...value, price: event.target.value }))}
                />
                <Input
                  label="Stock quantity"
                  type="number"
                  required
                  min="0"
                  step="1"
                  value={form.stockQuantity}
                  onChange={(event) => setForm((value) => ({ ...value, stockQuantity: event.target.value }))}
                />
                <Input
                  label="Currency"
                  required
                  minLength={3}
                  maxLength={3}
                  value={form.currency}
                  onChange={(event) => setForm((value) => ({ ...value, currency: event.target.value.toUpperCase() }))}
                />
                <div className="space-y-1.5">
                  <label htmlFor="product-category" className="block text-xs font-semibold uppercase tracking-wider text-ink-muted">Category</label>
                  <select
                    id="product-category"
                    value={form.categoryId}
                    onChange={(event) => setForm((value) => ({ ...value, categoryId: event.target.value }))}
                    className="h-10 w-full rounded-lg border border-border bg-surface px-3 text-sm text-ink focus:border-accent focus:outline-none focus:ring-2 focus:ring-accent/20"
                  >
                    <option value="">Uncategorised</option>
                    {categories.map((category) => <option key={category.id} value={category.id}>{category.name}</option>)}
                  </select>
                </div>
              </div>

              <div className="space-y-1.5">
                <label htmlFor="product-description" className="block text-xs font-semibold uppercase tracking-wider text-ink-muted">Description</label>
                <textarea
                  id="product-description"
                  rows={3}
                  maxLength={4000}
                  value={form.description}
                  onChange={(event) => setForm((value) => ({ ...value, description: event.target.value }))}
                  className="w-full rounded-lg border border-border bg-surface px-3 py-2 text-sm text-ink focus:border-accent focus:outline-none focus:ring-2 focus:ring-accent/20"
                />
              </div>

              <Input
                label="Tags"
                value={form.tags}
                onChange={(event) => setForm((value) => ({ ...value, tags: event.target.value }))}
                helperText="Comma-separated, for example: monitor, gaming, usb-c"
              />

              <div className="space-y-1.5">
                <label htmlFor="product-metadata" className="block text-xs font-semibold uppercase tracking-wider text-ink-muted">Metadata JSON</label>
                <textarea
                  id="product-metadata"
                  rows={5}
                  spellCheck={false}
                  placeholder={'{"brand":"Nova","colour":"Black"}'}
                  value={form.metadata}
                  onChange={(event) => setForm((value) => ({ ...value, metadata: event.target.value }))}
                  className="w-full rounded-lg border border-border bg-surface px-3 py-2 font-mono text-sm text-ink focus:border-accent focus:outline-none focus:ring-2 focus:ring-accent/20"
                />
              </div>

              <label className="flex items-center gap-3 text-sm text-ink">
                <input
                  type="checkbox"
                  checked={form.isActive}
                  onChange={(event) => setForm((value) => ({ ...value, isActive: event.target.checked }))}
                  className="h-4 w-4 rounded border-border text-accent"
                />
                Active and visible in the storefront
              </label>

              {formError && <p className="rounded-lg bg-bg-subtle px-3 py-2 text-sm text-danger">{formError}</p>}

              <div className="flex justify-end gap-3 border-t border-border pt-5">
                <Button type="button" variant="secondary" onClick={closeForm} disabled={submitting}>Cancel</Button>
                <Button type="submit" disabled={submitting}>{submitting ? 'Saving…' : editing ? 'Save changes' : 'Create product'}</Button>
              </div>
            </form>
          </Card>
        </div>
      )}
    </div>
  );
}
