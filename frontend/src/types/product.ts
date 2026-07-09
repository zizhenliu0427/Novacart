export interface Product {
  id: number | string;
  name: string;
  price: number;
  category: string;
  description: string;
  /** Optional compare-at price for sale display. */
  compareAtPrice?: number;
}

export function formatPrice(value: number, currency = 'AUD'): string {
  return new Intl.NumberFormat('en-AU', { style: 'currency', currency }).format(value);
}
