'use client';

import { useLocale, useTranslations } from 'next-intl';
import { useCurrency } from '@/contexts/CurrencyContext';
import { CURRENCY_LABELS, DISPLAY_CURRENCIES, type DisplayCurrency } from '@/types/currency';
import type { AppLocale } from '@/i18n/routing';

export function CurrencySwitcher() {
  const t = useTranslations('currency');
  const locale = useLocale() as AppLocale;
  const { displayCurrency, setDisplayCurrency, loading, rateDate, rates } = useCurrency();

  return (
    <div className="flex flex-col items-end gap-0.5">
      <label className="sr-only" htmlFor="currency-select">
        {t('label')}
      </label>
      <select
        id="currency-select"
        value={displayCurrency}
        disabled={loading && !rates}
        onChange={(e) => setDisplayCurrency(e.target.value as DisplayCurrency)}
        className="rounded-lg border border-border bg-surface px-2 py-1.5 text-xs font-medium text-ink-muted transition hover:bg-bg-subtle focus:outline-none focus:ring-2 focus:ring-accent/40"
        title={rateDate ? t('rateDate', { date: rateDate }) : t('loadingRates')}
      >
        {DISPLAY_CURRENCIES.map((code) => (
          <option key={code} value={code}>
            {CURRENCY_LABELS[code][locale === 'zh' ? 'zh' : 'en']}
          </option>
        ))}
      </select>
    </div>
  );
}
