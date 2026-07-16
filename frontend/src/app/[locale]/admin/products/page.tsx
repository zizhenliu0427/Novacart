'use client';

import { FormEvent, useCallback, useEffect, useState } from 'react';
import { useTranslations } from 'next-intl';
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
import { useFormatAudPrice } from '@/hooks/useFormatAudPrice';

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
  imageUrl: string;
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
  imageUrl: '',
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
    imageUrl: product.imageUrl ?? '',
    isActive: product.isActive,
  };
}

export default function AdminProductsPage() {
  const t = useTranslations('adminProducts');
  const tc = useTranslations('common');
  const { user } = useAuth();
  const { formatAud } = useFormatAudPrice();
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
  const [uploading, setUploading] = useState(false);

  async function handleImageUpload(event: React.ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0];
    if (!file) return;
    setUploading(true);
    setFormError(null);
    try {
      // 1. Get a presigned PUT URL from the backend (uses S3/LocalStack).
      const presign = await apiCall<{ uploadUrl: string; publicUrl: string; objectKey: string }>(
        '/admin/uploads/presign',
        { method: 'POST', body: { fileName: file.name, contentType: file.type } },
      );
      // 2. Upload directly to S3 (bypasses the backend entirely).
      const putRes = await fetch(presign.uploadUrl, {
        method: 'PUT',
        body: file,
        headers: { 'Content-Type': file.type },
      });
      if (!putRes.ok) throw new Error(`Upload failed (${putRes.status})`);
      // 3. Store the resulting public URL in the form.
      setForm((value) => ({ ...value, imageUrl: presign.publicUrl }));
    } catch (err) {
      setFormError(err instanceof Error ? t('errUploadDetail', { message: err.message }) : t('errUpload'));
    } finally {
      setUploading(false);
      // Reset so selecting the same file again re-triggers the handler.
      event.target.value = '';
    }
  }

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
      setError(err instanceof Error ? err.message : t('errSync'));
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
      setError(err instanceof Error ? err.message : t('errLoad'));
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
      .catch((err) => setError(err instanceof Error ? err.message : t('errCategories')));
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
      setFormError(t('errPrice'));
      return;
    }
    if (!Number.isInteger(stockQuantity) || stockQuantity < 0) {
      setFormError(t('errStock'));
      return;
    }
    if (form.metadata.trim()) {
      try {
        const parsed: unknown = JSON.parse(form.metadata);
        if (!parsed || Array.isArray(parsed) || typeof parsed !== 'object') throw new Error();
      } catch {
        setFormError(t('errMetadata'));
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
      imageUrl: form.imageUrl.trim() || undefined,
      isActive: form.isActive,
    };

    setSubmitting(true);
    try {
      if (editing) {
        await apiCall<AdminProduct>(`/admin/products/${editing.id}`, {
          method: 'PUT', body: request,
        });
        setNotice(t('noticeUpdated', { name: request.name }));
      } else {
        await apiCall<AdminProduct>('/admin/products', {
          method: 'POST', body: request,
        });
        setNotice(t('noticeCreated', { name: request.name }));
      }
      setFormOpen(false);
      setEditing(null);
      await loadProducts();
    } catch (err) {
      setFormError(err instanceof Error ? err.message : t('errSave'));
    } finally {
      setSubmitting(false);
    }
  }

  async function deactivate(product: AdminProduct) {
    if (!user || !window.confirm(t('confirmDeactivate', { name: product.name }))) return;
    setError(null);
    try {
      await apiCall<void>(`/admin/products/${product.id}`, { method: 'DELETE' });
      setNotice(t('noticeDeactivated', { name: product.name }));
      await loadProducts();
    } catch (err) {
      setError(err instanceof Error ? err.message : t('errDeactivate'));
    }
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight text-ink">{t('title')}</h1>
          <p className="mt-1 text-sm text-ink-muted">
            {loading ? t('loadingCatalogue') : t('productCount', { count: totalCount })}
          </p>
        </div>
        <div className="flex gap-2">
          <Button variant="secondary" onClick={triggerSquareSync} disabled={syncing}>
            {syncing ? t('syncing') : t('syncSquare')}
          </Button>
          <Button onClick={openCreate}>{t('addProduct')}</Button>
        </div>
      </div>

      {notice && (
        <div className="flex items-center justify-between rounded-lg border border-border bg-bg-subtle px-4 py-3 text-sm text-success">
          <span>{notice}</span>
          <button onClick={() => setNotice(null)} className="text-ink-muted hover:text-ink" aria-label={t('dismiss')}>×</button>
        </div>
      )}

      <div className="flex flex-wrap gap-3">
        <div className="min-w-64 flex-1">
          <Input
            type="search"
            value={query}
            onChange={(event) => setQuery(event.target.value)}
            placeholder={t('searchPlaceholder')}
            aria-label={t('searchAria')}
          />
        </div>
        <select
          value={status}
          onChange={(event) => { setStatus(event.target.value as StatusFilter); setPage(1); }}
          className="h-10 rounded-lg border border-border bg-surface px-3 text-sm text-ink focus:border-accent focus:outline-none"
          aria-label={t('filterStatusAria')}
        >
          <option value="all">{t('statusAll')}</option>
          <option value="active">{t('statusActive')}</option>
          <option value="inactive">{t('statusInactive')}</option>
        </select>
      </div>

      {error && (
        <EmptyState icon={<GridIcon />} title={t('loadErrorTitle')} description={error} />
      )}

      {!error && (
        <Card className="overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full min-w-[760px] text-left text-sm">
              <thead className="border-b border-border bg-bg-subtle text-xs uppercase tracking-wide text-ink-muted">
                <tr>
                  <th className="px-4 py-3 font-semibold">{t('colProduct')}</th>
                  <th className="px-4 py-3 font-semibold">{t('colCategory')}</th>
                  <th className="px-4 py-3 font-semibold">{t('colPrice')}</th>
                  <th className="px-4 py-3 font-semibold">{t('colStock')}</th>
                  <th className="px-4 py-3 font-semibold">{t('colStatus')}</th>
                  <th className="px-4 py-3 text-right font-semibold">{t('colActions')}</th>
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
                    <td className="px-4 py-3 text-ink-muted">{product.categoryName ?? t('uncategorised')}</td>
                    <td className="px-4 py-3 font-medium text-ink tnum">{formatAud(product.price)}</td>
                    <td className="px-4 py-3">
                      <Badge tone={product.stockQuantity === 0 ? 'danger' : product.stockQuantity <= 10 ? 'warning' : 'neutral'}>
                        {product.stockQuantity === 0 ? t('outOfStock') : product.stockQuantity}
                      </Badge>
                    </td>
                    <td className="px-4 py-3">
                      <Badge tone={product.isActive ? 'success' : 'neutral'}>{product.isActive ? t('active') : t('inactive')}</Badge>
                    </td>
                    <td className="px-4 py-3">
                      <div className="flex justify-end gap-2">
                        <Button variant="secondary" size="sm" onClick={() => openEdit(product)}>{tc('edit')}</Button>
                        {product.isActive && (
                          <Button variant="ghost" size="sm" className="text-danger" onClick={() => deactivate(product)}>
                            {t('deactivate')}
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
            <div className="p-8 text-center text-sm text-ink-muted">{t('noResults')}</div>
          )}
          {!loading && totalPages > 1 && (
            <div className="flex items-center justify-between border-t border-border px-4 py-3">
              <Button variant="secondary" size="sm" disabled={page <= 1} onClick={() => setPage((value) => value - 1)}>
                {t('previous')}
              </Button>
              <span className="text-xs text-ink-muted">{t('pageOf', { page, total: totalPages })}</span>
              <Button variant="secondary" size="sm" disabled={page >= totalPages} onClick={() => setPage((value) => value + 1)}>
                {t('next')}
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
                <h2 id="product-form-title" className="text-xl font-semibold text-ink">{editing ? t('editProduct') : t('addProductTitle')}</h2>
                <p className="text-sm text-ink-muted">{t('formSubtitle')}</p>
              </div>
              <button onClick={closeForm} className="text-2xl leading-none text-ink-muted hover:text-ink" aria-label={t('close')}>×</button>
            </div>

            <form onSubmit={submitProduct} className="space-y-5">
              <div className="grid gap-4 sm:grid-cols-2">
                <Input
                  label={t('name')}
                  required
                  minLength={2}
                  maxLength={300}
                  value={form.name}
                  onChange={(event) => setForm((value) => ({ ...value, name: event.target.value, ...(!editing ? { slug: slugify(event.target.value) } : {}) }))}
                />
                <Input
                  label={t('slug')}
                  required
                  pattern="[a-z0-9]+(?:-[a-z0-9]+)*"
                  value={form.slug}
                  onChange={(event) => setForm((value) => ({ ...value, slug: event.target.value }))}
                  helperText={t('slugHelper')}
                />
                <Input
                  label={t('price')}
                  type="number"
                  required
                  min="0.01"
                  step="0.01"
                  value={form.price}
                  onChange={(event) => setForm((value) => ({ ...value, price: event.target.value }))}
                />
                <Input
                  label={t('stockQuantity')}
                  type="number"
                  required
                  min="0"
                  step="1"
                  value={form.stockQuantity}
                  onChange={(event) => setForm((value) => ({ ...value, stockQuantity: event.target.value }))}
                />
                <Input
                  label={t('currency')}
                  required
                  minLength={3}
                  maxLength={3}
                  value={form.currency}
                  onChange={(event) => setForm((value) => ({ ...value, currency: event.target.value.toUpperCase() }))}
                />
                <div className="space-y-1.5">
                  <label htmlFor="product-category" className="block text-xs font-semibold uppercase tracking-wider text-ink-muted">{t('category')}</label>
                  <select
                    id="product-category"
                    value={form.categoryId}
                    onChange={(event) => setForm((value) => ({ ...value, categoryId: event.target.value }))}
                    className="h-10 w-full rounded-lg border border-border bg-surface px-3 text-sm text-ink focus:border-accent focus:outline-none focus:ring-2 focus:ring-accent/20"
                  >
                    <option value="">{t('uncategorised')}</option>
                    {categories.map((category) => <option key={category.id} value={category.id}>{category.name}</option>)}
                  </select>
                </div>
              </div>

              <div className="space-y-1.5">
                <label htmlFor="product-description" className="block text-xs font-semibold uppercase tracking-wider text-ink-muted">{t('description')}</label>
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
                label={t('tags')}
                value={form.tags}
                onChange={(event) => setForm((value) => ({ ...value, tags: event.target.value }))}
                helperText={t('tagsHelper')}
              />

              <Input
                label={t('imageUrl')}
                type="url"
                value={form.imageUrl}
                onChange={(event) => setForm((value) => ({ ...value, imageUrl: event.target.value }))}
                helperText={t('imageUrlHelper')}
              />

              <div className="space-y-1.5">
                <label htmlFor="product-image-upload" className="block text-xs font-semibold uppercase tracking-wider text-ink-muted">{t('uploadImage')}</label>
                <input
                  id="product-image-upload"
                  type="file"
                  accept="image/*"
                  onChange={handleImageUpload}
                  disabled={uploading}
                  className="block w-full text-sm text-ink-muted file:mr-3 file:rounded-md file:border-0 file:bg-accent file:px-4 file:py-2 file:text-sm file:font-semibold file:text-accent-contrast hover:file:bg-accent-hover disabled:opacity-40"
                />
                {uploading && <p className="text-xs text-ink-muted">{t('uploading')}</p>}
              </div>

              <div className="space-y-1.5">
                <label htmlFor="product-metadata" className="block text-xs font-semibold uppercase tracking-wider text-ink-muted">{t('metadataJson')}</label>
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
                {t('activeVisible')}
              </label>

              {formError && <p className="rounded-lg bg-bg-subtle px-3 py-2 text-sm text-danger">{formError}</p>}

              <div className="flex justify-end gap-3 border-t border-border pt-5">
                <Button type="button" variant="secondary" onClick={closeForm} disabled={submitting}>{tc('cancel')}</Button>
                <Button type="submit" disabled={submitting}>{submitting ? t('saving') : editing ? t('saveChanges') : t('createProduct')}</Button>
              </div>
            </form>
          </Card>
        </div>
      )}
    </div>
  );
}
