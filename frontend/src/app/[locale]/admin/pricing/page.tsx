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

export default function AdminPricingPage() {
  const t = useTranslations('adminPricing');
  const tc = useTranslations('common');
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

  const formatDate = (iso: string | null) => {
    if (!iso) return '—';
    return new Date(iso).toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' });
  };

  const describeRule = (rule: PriceRule) => {
    switch (rule.ruleType) {
      case 'Percent': return t('rulePercent', { value: rule.value });
      case 'Flat': return t('ruleFlat', { value: rule.value.toFixed(2) });
      case 'Fixed': return t('ruleFixed', { value: rule.value.toFixed(2) });
      default: return String(rule.value);
    }
  };

  const scopeLabel = (rule: PriceRule) => {
    if (rule.productId) return t('scopeProduct', { name: rule.productName ?? rule.productId.slice(0, 8) });
    if (rule.categoryId) return t('scopeCategory', { name: rule.categoryName ?? String(rule.categoryId) });
    return t('scopeGlobal');
  };

  const ruleTypeOption = (type: RuleType) => {
    switch (type) {
      case 'Percent': return t('typePercent');
      case 'Flat': return t('typeFlat');
      case 'Fixed': return t('typeFixed');
    }
  };

  const loadRules = useCallback(async () => {
    if (!user) return;
    setLoading(true);
    setError(null);
    try {
      const data = await apiCall<PriceRule[]>('/admin/price-rules');
      setRules(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : t('errLoad'));
    } finally {
      setLoading(false);
    }
  }, [user, t]);

  useEffect(() => {
    loadRules();
  }, [loadRules]);

  useEffect(() => {
    if (!user) return;
    apiCall<CategoryOption[]>('/admin/products/categories')
      .then(setCategories)
      .catch(() => {});
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
      setFormError(t('errValue'));
      return;
    }
    if (form.ruleType === 'Percent' && value > 100) {
      setFormError(t('errPercent'));
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
      setNotice(t('noticeCreated'));
      setFormOpen(false);
      await loadRules();
    } catch (err) {
      setFormError(err instanceof Error ? err.message : t('errSave'));
    } finally {
      setSubmitting(false);
    }
  }

  async function deleteRule(rule: PriceRule) {
    if (!user) return;
    if (!window.confirm(t('confirmDelete', { description: describeRule(rule) }))) return;
    setError(null);
    try {
      await apiCall<void>(`/admin/price-rules/${rule.id}`, { method: 'DELETE' });
      setNotice(t('noticeDeleted'));
      await loadRules();
    } catch (err) {
      setError(err instanceof Error ? err.message : t('errDelete'));
    }
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight text-ink">{t('title')}</h1>
          <p className="mt-1 text-sm text-ink-muted">
            {loading ? t('loadingRules') : t('ruleCount', { count: rules.length })}
          </p>
        </div>
        <Button onClick={openCreate}>{t('addRule')}</Button>
      </div>

      {notice && (
        <div className="flex items-center justify-between rounded-lg border border-border bg-bg-subtle px-4 py-3 text-sm text-success">
          <span>{notice}</span>
          <button onClick={() => setNotice(null)} className="text-ink-muted hover:text-ink" aria-label={t('dismiss')}>×</button>
        </div>
      )}

      {error && (
        <EmptyState icon={<GridIcon />} title={t('loadErrorTitle')} description={error} />
      )}

      {!error && (
        <Card className="overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full min-w-[680px] text-left text-sm">
              <thead className="border-b border-border bg-bg-subtle text-xs uppercase tracking-wide text-ink-muted">
                <tr>
                  <th className="px-4 py-3 font-semibold">{t('colScope')}</th>
                  <th className="px-4 py-3 font-semibold">{t('colType')}</th>
                  <th className="px-4 py-3 font-semibold">{t('colValue')}</th>
                  <th className="px-4 py-3 font-semibold">{t('colWindow')}</th>
                  <th className="px-4 py-3 font-semibold">{t('colStatus')}</th>
                  <th className="px-4 py-3 text-right font-semibold">{t('colActions')}</th>
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
                        ? t('windowRange', { start: formatDate(rule.startsAt), end: formatDate(rule.endsAt) })
                        : t('noLimit')}
                    </td>
                    <td className="px-4 py-3">
                      <Badge tone={rule.isActive ? 'success' : 'neutral'}>
                        {rule.isActive ? t('active') : t('inactive')}
                      </Badge>
                    </td>
                    <td className="px-4 py-3">
                      <div className="flex justify-end">
                        <Button variant="ghost" size="sm" className="text-danger" onClick={() => deleteRule(rule)}>
                          {t('delete')}
                        </Button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          {!loading && rules.length === 0 && (
            <div className="p-8 text-center text-sm text-ink-muted">{t('noRules')}</div>
          )}
        </Card>
      )}

      {formOpen && (
        <div className="fixed inset-0 z-50 flex items-start justify-center overflow-y-auto bg-black/50 p-4 sm:p-8" role="dialog" aria-modal="true" aria-labelledby="rule-form-title">
          <Card className="w-full max-w-lg p-5 sm:p-6">
            <div className="mb-5 flex items-start justify-between gap-4">
              <div>
                <h2 id="rule-form-title" className="text-xl font-semibold text-ink">{t('addRuleTitle')}</h2>
                <p className="text-sm text-ink-muted">{t('formSubtitle')}</p>
              </div>
              <button onClick={closeForm} className="text-2xl leading-none text-ink-muted hover:text-ink" aria-label={t('close')}>×</button>
            </div>

            <form onSubmit={submitRule} className="space-y-5">
              <div className="space-y-1.5">
                <label className="block text-xs font-semibold uppercase tracking-wider text-ink-muted">{t('scope')}</label>
                <div className="flex gap-4 text-sm">
                  {(['global', 'category', 'product'] as const).map((s) => (
                    <label key={s} className="flex items-center gap-2 text-ink">
                      <input
                        type="radio"
                        name="scope"
                        checked={form.scope === s}
                        onChange={() => setForm((value) => ({ ...value, scope: s }))}
                        className="h-4 w-4 border-border text-accent"
                      />
                      {s === 'global' ? t('scopeGlobalLabel') : s === 'category' ? t('scopeCategoryLabel') : t('scopeProductLabel')}
                    </label>
                  ))}
                </div>
              </div>

              {form.scope === 'category' && (
                <div className="space-y-1.5">
                  <label htmlFor="rule-category" className="block text-xs font-semibold uppercase tracking-wider text-ink-muted">{t('scopeCategoryLabel')}</label>
                  <select
                    id="rule-category"
                    value={form.categoryId}
                    onChange={(event) => setForm((value) => ({ ...value, categoryId: event.target.value }))}
                    className="h-10 w-full rounded-lg border border-border bg-surface px-3 text-sm text-ink focus:border-accent focus:outline-none focus:ring-2 focus:ring-accent/20"
                    required
                  >
                    <option value="">{t('selectCategory')}</option>
                    {categories.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
                  </select>
                </div>
              )}

              {form.scope === 'product' && (
                <div className="space-y-1.5">
                  <label htmlFor="rule-product" className="block text-xs font-semibold uppercase tracking-wider text-ink-muted">{t('scopeProductLabel')}</label>
                  <select
                    id="rule-product"
                    value={form.productId}
                    onChange={(event) => setForm((value) => ({ ...value, productId: event.target.value }))}
                    className="h-10 w-full rounded-lg border border-border bg-surface px-3 text-sm text-ink focus:border-accent focus:outline-none focus:ring-2 focus:ring-accent/20"
                    required
                  >
                    <option value="">{t('selectProduct')}</option>
                    {products.map((p) => <option key={p.id} value={p.id}>{p.name}</option>)}
                  </select>
                </div>
              )}

              <div className="grid gap-4 sm:grid-cols-2">
                <div className="space-y-1.5">
                  <label className="block text-xs font-semibold uppercase tracking-wider text-ink-muted">{t('ruleType')}</label>
                  <select
                    value={form.ruleType}
                    onChange={(event) => setForm((value) => ({ ...value, ruleType: event.target.value as RuleType }))}
                    className="h-10 w-full rounded-lg border border-border bg-surface px-3 text-sm text-ink focus:border-accent focus:outline-none focus:ring-2 focus:ring-accent/20"
                  >
                    {(['Percent', 'Flat', 'Fixed'] as RuleType[]).map((type) => (
                      <option key={type} value={type}>{type} ({ruleTypeOption(type)})</option>
                    ))}
                  </select>
                </div>
                <Input
                  label={t('value')}
                  type="number"
                  required
                  min="0"
                  step="0.01"
                  value={form.value}
                  onChange={(event) => setForm((value) => ({ ...value, value: event.target.value }))}
                  helperText={form.ruleType === 'Percent' ? t('valuePercentHint') : t('valueAmountHint')}
                />
              </div>

              <div className="grid gap-4 sm:grid-cols-2">
                <Input
                  label={t('startsAt')}
                  type="date"
                  value={form.startsAt}
                  onChange={(event) => setForm((value) => ({ ...value, startsAt: event.target.value }))}
                />
                <Input
                  label={t('endsAt')}
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
                {t('active')}
              </label>

              {formError && <p className="rounded-lg bg-bg-subtle px-3 py-2 text-sm text-danger">{formError}</p>}

              <div className="flex justify-end gap-3 border-t border-border pt-5">
                <Button type="button" variant="secondary" onClick={closeForm} disabled={submitting}>{tc('cancel')}</Button>
                <Button type="submit" disabled={submitting}>{submitting ? t('saving') : t('createRule')}</Button>
              </div>
            </form>
          </Card>
        </div>
      )}
    </div>
  );
}
