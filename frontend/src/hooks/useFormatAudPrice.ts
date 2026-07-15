'use client';

import { useCurrency } from '@/contexts/CurrencyContext';

export function useFormatAudPrice() {
  const { formatAud, displayCurrency, rateDate } = useCurrency();
  return {
    formatAud,
    showDisclaimer: displayCurrency !== 'AUD',
    displayCurrency,
    rateDate,
  };
}
