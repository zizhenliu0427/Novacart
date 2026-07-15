/** Display currencies supported in the storefront (base catalogue prices are always AUD). */
export type DisplayCurrency = 'AUD' | 'USD' | 'CNY' | 'JPY' | 'SGD' | 'GBP' | 'NZD';

export const DISPLAY_CURRENCIES: DisplayCurrency[] = ['AUD', 'USD', 'CNY', 'JPY', 'SGD', 'GBP', 'NZD'];

export interface ExchangeRates {
  base: string;
  date: string;
  rates: Record<string, number>;
  source: string;
}

export const CURRENCY_LABELS: Record<DisplayCurrency, { en: string; zh: string }> = {
  AUD: { en: 'AUD', zh: '澳元' },
  USD: { en: 'USD', zh: '美元' },
  CNY: { en: 'RMB (CNY)', zh: '人民币' },
  JPY: { en: 'JPY', zh: '日元' },
  SGD: { en: 'SGD', zh: '新元' },
  GBP: { en: 'GBP', zh: '英镑' },
  NZD: { en: 'NZD', zh: '新西兰元' },
};
