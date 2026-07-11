'use client';

/* ─── Types ────────────────────────────────────────────────────── */
export type ToastVariant = 'success' | 'error' | 'info';

export interface ToastData {
  id: string;
  message: string;
  variant: ToastVariant;
}

export interface ToastProps {
  toast: ToastData;
  onDismiss: (id: string) => void;
}

/* ─── Variant styles ───────────────────────────────────────────── */
const variantStyles: Record<ToastVariant, { accent: string; icon: string }> = {
  success: {
    accent: 'border-l-[var(--success)]',
    icon: '✓',
  },
  error: {
    accent: 'border-l-[var(--danger)]',
    icon: '✕',
  },
  info: {
    accent: 'border-l-[var(--accent)]',
    icon: 'ℹ',
  },
};

const variantIconBg: Record<ToastVariant, string> = {
  success: 'bg-[var(--success)]',
  error: 'bg-[var(--danger)]',
  info: 'bg-[var(--accent)]',
};

/* ─── Toast ────────────────────────────────────────────────────── */
export function Toast({ toast, onDismiss }: ToastProps) {
  const { accent, icon } = variantStyles[toast.variant];
  const iconBg = variantIconBg[toast.variant];

  return (
    <div
      role="alert"
      className={`pointer-events-auto flex w-80 items-start gap-3 rounded-lg border border-border bg-surface
        px-4 py-3 shadow-hover animate-[slideInRight_250ms_ease-out] border-l-4 ${accent}`}
    >
      {/* Icon badge */}
      <span
        className={`mt-0.5 flex h-5 w-5 flex-shrink-0 items-center justify-center rounded-full text-xs font-bold text-white ${iconBg}`}
      >
        {icon}
      </span>

      {/* Message */}
      <p className="flex-1 text-sm text-ink">{toast.message}</p>

      {/* Dismiss */}
      <button
        onClick={() => onDismiss(toast.id)}
        className="flex-shrink-0 text-lg leading-none text-ink-muted transition hover:text-ink"
        aria-label="Dismiss notification"
      >
        ×
      </button>
    </div>
  );
}
