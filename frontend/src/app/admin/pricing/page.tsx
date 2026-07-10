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
import type { CategoryOption } from '@/types/product';

type RuleType = 'Percent' | 'Flat' | 'Fixed';

interface PriceRule {
  id: string;
  productId: string | null;
  productName: string | null;
  categoryId: number | null;
  categoryName: string | null;
  ruleType: RuleType;
  value: number;
  startsAt: string | null;
  endsAt: string | null;
  isActive: boolean;
  createdAt: string;
}

interface CreatePriceRuleBody {
  productId?: string;
  categoryId?: number;
  ruleType: RuleType;
  value: number;
  startsAt?: string;
  endsAt?: string;
  isActive: boolean;
}

interface FormState {
  scope: 'global' | 'category' | 'product';
  categoryId: string;
  productId: string;
  ruleType: RuleType;
  value: string;
  startsAt: string;
  endsAt: string;
  isActive: boolean;
}

const EMPTY_FORM: FormState = {
  scope: 'global',
  categoryId: '',
  productId: '',
  ruleType: 'Percent',
  value: '',
  startsAt: '',
  endsAt: '',
  isActive: true,
};

const RULE_TYPE_LABEL: Record<RuleType, string> = {
  Percent: '% off',
  Flat: 'flat off',
  Fixed: 'fixed price',
};

function formatDate(iso: string | null): string {
  if (!iso) return '—';
  return new Date(iso).toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' });
}

function describeRule(rule: PriceRule): string {
  switch (rule.ruleType) {
    case 'Percent': return `${rule.value}% off`;
    case 'Flat': return `$${rule.value.toFixed(2)} off`;
    case 'Fixed': return `$${rule.value.toFixed(2)}`;
    default: return `${rule.value}`;
  }
}

function scopeLabel(rule: PriceRule): string {
  if (rule.productId) return `Product: ${rule.productName ?? rule.productId.slice(0, 8)}`;
  if (rule.categoryId) return `Category: ${rule.categoryName ?? rule.categoryId}`;
  return 'Global';
}

export default function AdminPricingPage() {
  const { user } = useAuth();
  const [rules, setRules] = useState<PriceRule[]>([]);
  const [categories, setCategories] = useState<CategoryOption[]>([]);
  const [products, setProducts] = useState<{ id: string; name: string }[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);

  const [formOpen, setFormOpen] = useState(false);
  const [form, setForm] = useState<FormState>(EMPTY_FORM);
  const [formError, setFormError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const loadRules = useCallback(async () => {
    if (!user) return;
    setLoading(true);
    setError(null);
    try {
      const data = await apiCall<PriceRule[]>('/admin/price-rules');
      setRules(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load pricing rules.');
    } finally {
      setLoading(false);
    }
  }, [user]);

  useEffect(() => {
    loadRules();
  }, [loadRules]);

  useEffect(() => {
    if (!user) return;
    apiCall<CategoryOption[]>('/admin/products/categories')
      .then(setCategories)
      .catch(() => {});
    // Load products for the product-scoped selector (reuse admin products endpoint).
    apiCall<{ items: { id: string; name: string }[] }>('/admin/products?pageSize=100')
      .then((data) => setProducts(data.items))
      .catch(() => {});
  }, [user]);

  function openCreate() {
    setForm(EMPTY_FORM);
    setFormError(null);
    setFormOpen(true);
  }

  function closeForm() {
    if (submitting) return;
    setFormOpen(false);
    setFormError(null);
  }

  async function submitRule(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!user) return;
    setFormError(null);

    const value = Number(form.value);
    if (!Number.isFinite(value) || value < 0) {
      setFormError('Value must be zero or greater.');
      return;
    }
    if (form.ruleType === 'Percent' && value > 100) {
      setFormError('Percentage must be between 0 and 100.');
      return;
    }

    const body: CreatePriceRuleBody = {
      ruleType: form.ruleType,
      value,
      isActive: form.isActive,
    };
    if (form.scope === 'category' && form.categoryId) body.categoryId = Number(form.categoryId);
    if (form.scope === 'product' && form.productId) body.productId = form.productId;
    if (form.startsAt) body.startsAt = new Date(form.startsAt).toISOString();
    if (form.endsAt) body.endsAt = new Date(form.endsAt).toISOString();

    setSubmitting(true);
    try {
      await apiCall<PriceRule>('/admin/price-rules', { method: 'POST', body });
      setNotice('Pricing rule created.');
      setFormOpen(false);
      await loadRules();
    } catch (err) {
      setFormError(err instanceof Error ? err.message : 'Failed to save pricing rule.');
    } finally {
      setSubmitting(false);
    }
  }

  async function deleteRule(rule: PriceRule) {
    if (!user) return;
    if (!window.confirm(`Delete this ${describeRule(rule)} rule?`)) return;
    setError(null);
    try {
      await apiCall<void>(`/admin/price-rules/${rule.id}`, { method: 'DELETE' });
      setNotice('Pricing rule deleted.');
      await loadRules();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to delete rule.');
    }
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight text-ink">Pricing rules</h1>
          <p className="mt-1 text-sm text-ink-muted">
            {loading ? 'Loading rules…' : `${rules.length} active rule${rules.length === 1 ? '' : 's'}`}
          </p>
        </div>
        <Button onClick={openCreate}>Add rule</Button>
      </div>

      {notice && (
        <div className="flex items-center justify-between rounded-lg border border-border bg-bg-subtle px-4 py-3 text-sm text-success">
          <span>{notice}</span>
          <button onClick={() => setNotice(null)} className="text-ink-muted hover:text-ink" aria-label="Dismiss">×</button>
        </div>
      )}

      {error && (
        <EmptyState icon={<GridIcon />} title="Couldn't load pricing rules" description={error} />
      )}

      {!error && (
        <Card className="overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full min-w-[680px] text-left text-sm">
              <thead className="border-b border-border bg-bg-subtle text-xs uppercase tracking-wide text-ink-muted">
                <tr>
                  <th className="px-4 py-3 font-semibold">Scope</th>
                  <th className="px-4 py-3 font-semibold">Type</th>
                  <th className="px-4 py-3 font-semibold">Value</th>
                  <th className="px-4 py-3 font-semibold">Window</th>
                  <th className="px-4 py-3 font-semibold">Status</th>
                  <th className="px-4 py-3 text-right font-semibold">Actions</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-border">
                {loading && Array.from({ length: 3 }).map((_, index) => (
                  <tr key={index} className="animate-pulse">
                    <td colSpan={6} className="px-4 py-4"><div className="h-5 rounded bg-bg-subtle" /></td>
                  </tr>
                ))}
                {!loading && rules.map((rule) => (
                  <tr key={rule.id} className="hover:bg-bg-subtle/60">
                    <td className="px-4 py-3 font-medium text-ink">{scopeLabel(rule)}</td>
                    <td className="px-4 py-3 text-ink-muted">{rule.ruleType}</td>
                    <td className="px-4 py-3 font-medium text-ink">{describeRule(rule)}</td>
                    <td className="px-4 py-3 text-ink-muted">
                      {rule.startsAt || rule.endsAt
                        ? `${formatDate(rule.startsAt)} → ${formatDate(rule.endsAt)}`
                        : 'No limit'}
                    </td>
                    <td className="px-4 py-3">
                      <Badge tone={rule.isActive ? 'success' : 'neutral'}>
                        {rule.isActive ? 'Active' : 'Inactive'}
                      </Badge>
                    </td>
                    <td className="px-4 py-3">
                      <div className="flex justify-end">
                        <Button variant="ghost" size="sm" className="text-danger" onClick={() => deleteRule(rule)}>
                          Delete
                        </Button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          {!loading && rules.length === 0 && (
            <div className="p-8 text-center text-sm text-ink-muted">No pricing rules configured yet.</div>
          )}
        </Card>
      )}

      {formOpen && (
        <div className="fixed inset-0 z-50 flex items-start justify-center overflow-y-auto bg-black/50 p-4 sm:p-8" role="dialog" aria-modal="true" aria-labelledby="rule-form-title">
          <Card className="w-full max-w-lg p-5 sm:p-6">
            <div className="mb-5 flex items-start justify-between gap-4">
              <div>
                <h2 id="rule-form-title" className="text-xl font-semibold text-ink">Add pricing rule</h2>
                <p className="text-sm text-ink-muted">Most-specific rule wins: product &gt; category &gt; global.</p>
              </div>
              <button onClick={closeForm} className="text-2xl leading-none text-ink-muted hover:text-ink" aria-label="Close">×</button>
            </div>

            <form onSubmit={submitRule} className="space-y-5">
              <div className="space-y-1.5">
                <label className="block text-xs font-semibold uppercase tracking-wider text-ink-muted">Scope</label>
                <div className="flex gap-4 text-sm">
                  {(['global', 'category', 'product'] as const).map((s) => (
                    <label key={s} className="flex items-center gap-2 capitalize text-ink">
                      <input
                        type="radio"
                        name="scope"
                        checked={form.scope === s}
                        onChange={() => setForm((value) => ({ ...value, scope: s }))}
                        className="h-4 w-4 border-border text-accent"
                      />
                      {s}
                    </label>
                  ))}
                </div>
              </div>

              {form.scope === 'category' && (
                <div className="space-y-1.5">
                  <label htmlFor="rule-category" className="block text-xs font-semibold uppercase tracking-wider text-ink-muted">Category</label>
                  <select
                    id="rule-category"
                    value={form.categoryId}
                    onChange={(event) => setForm((value) => ({ ...value, categoryId: event.target.value }))}
                    className="h-10 w-full rounded-lg border border-border bg-surface px-3 text-sm text-ink focus:border-accent focus:outline-none focus:ring-2 focus:ring-accent/20"
                    required
                  >
                    <option value="">Select a category…</option>
                    {categories.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
                  </select>
                </div>
              )}

              {form.scope === 'product' && (
                <div className="space-y-1.5">
                  <label htmlFor="rule-product" className="block text-xs font-semibold uppercase tracking-wider text-ink-muted">Product</label>
                  <select
                    id="rule-product"
                    value={form.productId}
                    onChange={(event) => setForm((value) => ({ ...value, productId: event.target.value }))}
                    className="h-10 w-full rounded-lg border border-border bg-surface px-3 text-sm text-ink focus:border-accent focus:outline-none focus:ring-2 focus:ring-accent/20"
                    required
                  >
                    <option value="">Select a product…</option>
                    {products.map((p) => <option key={p.id} value={p.id}>{p.name}</option>)}
                  </select>
                </div>
              )}

              <div className="grid gap-4 sm:grid-cols-2">
                <div className="space-y-1.5">
                  <label className="block text-xs font-semibold uppercase tracking-wider text-ink-muted">Rule type</label>
                  <select
                    value={form.ruleType}
                    onChange={(event) => setForm((value) => ({ ...value, ruleType: event.target.value as RuleType }))}
                    className="h-10 w-full rounded-lg border border-border bg-surface px-3 text-sm text-ink focus:border-accent focus:outline-none focus:ring-2 focus:ring-accent/20"
                  >
                    {(Object.keys(RULE_TYPE_LABEL) as RuleType[]).map((t) => (
                      <option key={t} value={t}>{t} ({RULE_TYPE_LABEL[t]})</option>
                    ))}
                  </select>
                </div>
                <Input
                  label="Value"
                  type="number"
                  required
                  min="0"
                  step="0.01"
                  value={form.value}
                  onChange={(event) => setForm((value) => ({ ...value, value: event.target.value }))}
                  helperText={form.ruleType === 'Percent' ? '0–100' : 'Currency amount'}
                />
              </div>

              <div className="grid gap-4 sm:grid-cols-2">
                <Input
                  label="Starts at (optional)"
                  type="date"
                  value={form.startsAt}
                  onChange={(event) => setForm((value) => ({ ...value, startsAt: event.target.value }))}
                />
                <Input
                  label="Ends at (optional)"
                  type="date"
                  value={form.endsAt}
                  onChange={(event) => setForm((value) => ({ ...value, endsAt: event.target.value }))}
                />
              </div>

              <label className="flex items-center gap-3 text-sm text-ink">
                <input
                  type="checkbox"
                  checked={form.isActive}
                  onChange={(event) => setForm((value) => ({ ...value, isActive: event.target.checked }))}
                  className="h-4 w-4 rounded border-border text-accent"
                />
                Active
              </label>

              {formError && <p className="rounded-lg bg-bg-subtle px-3 py-2 text-sm text-danger">{formError}</p>}

              <div className="flex justify-end gap-3 border-t border-border pt-5">
                <Button type="button" variant="secondary" onClick={closeForm} disabled={submitting}>Cancel</Button>
                <Button type="submit" disabled={submitting}>{submitting ? 'Saving…' : 'Create rule'}</Button>
              </div>
            </form>
          </Card>
        </div>
      )}
    </div>
  );
}
