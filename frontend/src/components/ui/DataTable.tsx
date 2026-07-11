'use client';

import { type ReactNode } from 'react';

/* ─── Column definition ────────────────────────────────────────── */
export interface Column<T> {
  /** Property key or unique identifier for the column. */
  key: string;
  /** Header label. */
  header: string;
  /** Optional custom cell renderer. Falls back to `String(row[key])`. */
  render?: (row: T, index: number) => ReactNode;
  /** Text alignment — defaults to `'left'`. */
  align?: 'left' | 'center' | 'right';
}

/* ─── Component props ──────────────────────────────────────────── */
export interface DataTableProps<T> {
  columns: Column<T>[];
  data: T[];
  /** Show skeleton loading rows. */
  loading?: boolean;
  /** Number of skeleton rows to display while loading (default 5). */
  skeletonRows?: number;
  /** Message displayed when `data` is empty and not loading. */
  emptyMessage?: string;
  className?: string;
}

/* ─── Alignment → Tailwind utility ─────────────────────────────── */
const alignClass = (align?: 'left' | 'center' | 'right') => {
  if (align === 'right') return 'text-right';
  if (align === 'center') return 'text-center';
  return 'text-left';
};

/* ─── DataTable ────────────────────────────────────────────────── */
export function DataTable<T extends Record<string, unknown>>({
  columns,
  data,
  loading = false,
  skeletonRows = 5,
  emptyMessage = 'No data to display.',
  className = '',
}: DataTableProps<T>) {
  return (
    <div className={`overflow-x-auto ${className}`}>
      <table className="w-full min-w-[640px] text-left text-sm">
        {/* ── Head ─────────────────────────────────────────────── */}
        <thead className="border-b border-border bg-bg-subtle text-xs uppercase tracking-wide text-ink-muted">
          <tr>
            {columns.map((col) => (
              <th
                key={col.key}
                className={`px-4 py-3 font-semibold ${alignClass(col.align)}`}
              >
                {col.header}
              </th>
            ))}
          </tr>
        </thead>

        {/* ── Body ─────────────────────────────────────────────── */}
        <tbody className="divide-y divide-border">
          {/* Loading skeletons */}
          {loading &&
            Array.from({ length: skeletonRows }).map((_, i) => (
              <tr key={`skel-${i}`} className="animate-pulse">
                <td colSpan={columns.length} className="px-4 py-4">
                  <div className="h-5 rounded bg-bg-subtle" />
                </td>
              </tr>
            ))}

          {/* Data rows */}
          {!loading &&
            data.map((row, rowIndex) => (
              <tr key={rowIndex} className="hover:bg-bg-subtle/60">
                {columns.map((col) => (
                  <td
                    key={col.key}
                    className={`px-4 py-3 ${alignClass(col.align)}`}
                  >
                    {col.render
                      ? col.render(row, rowIndex)
                      : String(row[col.key] ?? '')}
                  </td>
                ))}
              </tr>
            ))}
        </tbody>
      </table>

      {/* ── Empty state ──────────────────────────────────────── */}
      {!loading && data.length === 0 && (
        <div className="p-8 text-center text-sm text-ink-muted">
          {emptyMessage}
        </div>
      )}
    </div>
  );
}
