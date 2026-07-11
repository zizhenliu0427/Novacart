'use client';

import { Button } from './Button';

/* ─── Props ────────────────────────────────────────────────────── */
export interface PaginationProps {
  /** Current page (1-indexed). */
  page: number;
  /** Total number of pages. */
  totalPages: number;
  /** Called when the user requests a different page. */
  onPageChange: (page: number) => void;
  className?: string;
}

/* ─── Helpers ──────────────────────────────────────────────────── */

/**
 * Build a list of page numbers + ellipsis markers.
 * Always includes first, last, and a window around the current page.
 */
function buildPageRange(current: number, total: number): (number | '…')[] {
  if (total <= 7) {
    return Array.from({ length: total }, (_, i) => i + 1);
  }

  const pages = new Set<number>();
  pages.add(1);
  pages.add(total);
  for (let i = Math.max(2, current - 1); i <= Math.min(total - 1, current + 1); i++) {
    pages.add(i);
  }

  const sorted = [...pages].sort((a, b) => a - b);
  const result: (number | '…')[] = [];

  for (let i = 0; i < sorted.length; i++) {
    if (i > 0 && sorted[i] - sorted[i - 1] > 1) {
      result.push('…');
    }
    result.push(sorted[i]);
  }

  return result;
}

/* ─── Pagination ───────────────────────────────────────────────── */
export function Pagination({
  page,
  totalPages,
  onPageChange,
  className = '',
}: PaginationProps) {
  if (totalPages <= 1) return null;

  const range = buildPageRange(page, totalPages);

  return (
    <div
      className={`flex items-center justify-between gap-2 border-t border-border px-4 py-3 ${className}`}
    >
      <Button
        variant="secondary"
        size="sm"
        disabled={page <= 1}
        onClick={() => onPageChange(page - 1)}
        aria-label="Previous page"
      >
        Previous
      </Button>

      <div className="flex items-center gap-1">
        {range.map((entry, i) =>
          entry === '…' ? (
            <span key={`ellipsis-${i}`} className="px-1 text-xs text-ink-muted select-none">
              …
            </span>
          ) : (
            <button
              key={entry}
              onClick={() => onPageChange(entry)}
              disabled={entry === page}
              aria-current={entry === page ? 'page' : undefined}
              className={`inline-flex h-8 min-w-[2rem] items-center justify-center rounded-lg text-sm font-medium transition ${
                entry === page
                  ? 'bg-accent text-accent-contrast cursor-default'
                  : 'text-ink-muted hover:bg-bg-subtle hover:text-ink'
              }`}
            >
              {entry}
            </button>
          ),
        )}
      </div>

      <Button
        variant="secondary"
        size="sm"
        disabled={page >= totalPages}
        onClick={() => onPageChange(page + 1)}
        aria-label="Next page"
      >
        Next
      </Button>
    </div>
  );
}
