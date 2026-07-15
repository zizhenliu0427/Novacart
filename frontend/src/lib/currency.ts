import { intlLocaleFor, type AppLocale } from '@/i18n/routing';
import type { DisplayCurrency } from '@/types/currency';

/** Convert an AUD catalogue amount to the selected display currency. */
export function convertFromAud(
  audAmount: number,
  displayCurrency: DisplayCurrency,
  rates: Record<string, number> | null | undefined,
): number {
  if (displayCurrency === 'AUD') return audAmount;
  const rate = rates?.[displayCurrency];
  if (rate == null) return audAmount;
  return audAmount * rate;
}

/** Format an AUD catalogue amount in the user's selected display currency. */
export function formatAudAsDisplay(
  audAmount: number,
  displayCurrency: DisplayCurrency,
  rates: Record<string, number> | null | undefined,
  locale: AppLocale,
): string {
  const converted = convertFromAud(audAmount, displayCurrency, rates);
  const intlLocale = intlLocaleFor(locale);
  return new Intl.NumberFormat(intlLocale, {
    style: 'currency',
    currency: displayCurrency,
    maximumFractionDigits: displayCurrency === 'JPY' ? 0 : 2,
  }).format(converted);
}
