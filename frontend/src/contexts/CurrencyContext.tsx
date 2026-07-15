'use client';

import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
} from 'react';
import { useLocale } from 'next-intl';
import { apiCall } from '@/lib/api';
import { convertFromAud, formatAudAsDisplay } from '@/lib/currency';
import { type AppLocale } from '@/i18n/routing';
import {
  DISPLAY_CURRENCIES,
  type DisplayCurrency,
  type ExchangeRates,
} from '@/types/currency';

const STORAGE_KEY = 'novacart_display_currency';

interface CurrencyContextValue {
  displayCurrency: DisplayCurrency;
  setDisplayCurrency: (currency: DisplayCurrency) => void;
  rates: ExchangeRates | null;
  loading: boolean;
  convertFromAud: (audAmount: number) => number;
  formatAud: (audAmount: number) => string;
  rateDate: string | null;
}

const CurrencyContext = createContext<CurrencyContextValue | null>(null);

function readStoredCurrency(): DisplayCurrency {
  if (typeof window === 'undefined') return 'AUD';
  const stored = localStorage.getItem(STORAGE_KEY);
  if (stored && DISPLAY_CURRENCIES.includes(stored as DisplayCurrency)) {
    return stored as DisplayCurrency;
  }
  return 'AUD';
}

export function CurrencyProvider({ children }: { children: React.ReactNode }) {
  const locale = useLocale() as AppLocale;
  const [displayCurrency, setDisplayCurrencyState] = useState<DisplayCurrency>('AUD');
  const [rates, setRates] = useState<ExchangeRates | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    setDisplayCurrencyState(readStoredCurrency());
  }, []);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const data = await apiCall<ExchangeRates>('/currency/rates');
        if (!cancelled) setRates(data);
      } catch {
        if (!cancelled) setRates(null);
      } finally {
        if (!cancelled) setLoading(false);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, []);

  const setDisplayCurrency = useCallback((currency: DisplayCurrency) => {
    setDisplayCurrencyState(currency);
    localStorage.setItem(STORAGE_KEY, currency);
  }, []);

  const convertFromAudAmount = useCallback(
    (audAmount: number) => convertFromAud(audAmount, displayCurrency, rates?.rates),
    [displayCurrency, rates],
  );

  const formatAud = useCallback(
    (audAmount: number) => formatAudAsDisplay(audAmount, displayCurrency, rates?.rates, locale),
    [displayCurrency, rates, locale],
  );

  const value = useMemo<CurrencyContextValue>(
    () => ({
      displayCurrency,
      setDisplayCurrency,
      rates,
      loading,
      convertFromAud: convertFromAudAmount,
      formatAud,
      rateDate: rates?.date ?? null,
    }),
    [displayCurrency, setDisplayCurrency, rates, loading, convertFromAudAmount, formatAud],
  );

  return <CurrencyContext.Provider value={value}>{children}</CurrencyContext.Provider>;
}

export function useCurrency(): CurrencyContextValue {
  const ctx = useContext(CurrencyContext);
  if (!ctx) throw new Error('useCurrency must be used inside <CurrencyProvider>');
  return ctx;
}
