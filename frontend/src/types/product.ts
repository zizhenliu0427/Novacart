export interface Product {
  id: string;
  slug: string;
  name: string;
  price: number;
  currency: string;
  stockQuantity: number;
  categoryId?: number;
  categoryName?: string;
  description?: string;
  tags: string[];
  /** Optional compare-at price for sale display. */
  compareAtPrice?: number;
  /** Raw JSON string from the DB — rendered as a dynamic attribute table. */
  metadata?: string;
  imageUrl?: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface AdminProduct extends Product {
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CategoryOption {
  id: number;
  name: string;
  slug: string;
}

export interface AdminProductRequest {
  name: string;
  slug: string;
  description?: string;
  price: number;
  currency: string;
  stockQuantity: number;
  categoryId?: number;
  tags: string[];
  metadata?: string;
  imageUrl?: string;
  isActive: boolean;
}

export function formatPrice(value: number, currency = 'AUD', locale?: string): string {
  const intlLocale = locale === 'zh' ? 'zh-CN' : 'en-AU';
  return new Intl.NumberFormat(intlLocale, { style: 'currency', currency }).format(value);
}

/** Parse the raw metadata JSON string into a key→value record for display. */
export function parseMetadata(raw: string | undefined | null): Record<string, unknown> {
  if (!raw) return {};
  try {
    return JSON.parse(raw) as Record<string, unknown>;
  } catch {
    return {};
  }
}

/** Format a metadata value for display in the attribute table. */
export function formatMetadataValue(value: unknown): string {
  if (Array.isArray(value)) return value.join(', ');
  if (typeof value === 'boolean') return value ? 'Yes' : 'No';
  if (value === null || value === undefined) return '—';
  return String(value);
}

/** Convert a snake_case or camelCase key to Title Case for display. */
export function humanizeKey(key: string): string {
  return key
    .replace(/_/g, ' ')
    .replace(/([A-Z])/g, ' $1')
    .split(' ')
    .filter(Boolean)
    .map(word => word.charAt(0).toUpperCase() + word.slice(1).toLowerCase())
    .join(' ');
}
