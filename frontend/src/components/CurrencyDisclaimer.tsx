'use client';

import { useLocale, useTranslations } from 'next-intl';
import { useCurrency } from '@/contexts/CurrencyContext';
import { CURRENCY_LABELS, type DisplayCurrency } from '@/types/currency';
import type { AppLocale } from '@/i18n/routing';

/** Shown when prices are converted from AUD for display only. */
export function CurrencyDisclaimer({
  className = '',
  variant = 'checkout',
}: {
  className?: string;
  /** `checkout` = cart/checkout; `history` = past orders; `admin` = dashboard/reporting. */
  variant?: 'checkout' | 'history' | 'admin';
}) {
  const t = useTranslations('currency');
  const locale = useLocale() as AppLocale;
  const { displayCurrency, rateDate } = useCurrency();

  if (displayCurrency === 'AUD') return null;

  const label = CURRENCY_LABELS[displayCurrency as DisplayCurrency][locale === 'zh' ? 'zh' : 'en'];
  const messageKey =
    variant === 'history'
      ? 'historyDisclaimer'
      : variant === 'admin'
        ? 'adminDisclaimer'
        : 'checkoutDisclaimer';

  return (
    <p className={`text-xs text-ink-muted ${className}`.trim()}>
      {t(messageKey, { currency: label })}
      {rateDate ? ` ${t('rateDate', { date: rateDate })}` : ''}
    </p>
  );
}
