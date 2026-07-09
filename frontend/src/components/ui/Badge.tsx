type Tone = 'neutral' | 'accent' | 'success' | 'warning' | 'danger' | 'sale';

const tones: Record<Tone, string> = {
  neutral: 'bg-bg-subtle text-ink-muted',
  accent: 'bg-accent-weak text-accent',
  success: 'bg-bg-subtle text-success',
  warning: 'bg-bg-subtle text-warning',
  danger: 'bg-bg-subtle text-danger',
  sale: 'bg-sale text-white',
};

export function Badge({
  tone = 'neutral',
  className = '',
  children,
}: {
  tone?: Tone;
  className?: string;
  children: React.ReactNode;
}) {
  return (
    <span
      className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${tones[tone]} ${className}`}
    >
      {children}
    </span>
  );
}
