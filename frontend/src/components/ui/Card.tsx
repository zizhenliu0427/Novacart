interface CardProps extends React.HTMLAttributes<HTMLDivElement> {
  interactive?: boolean;
}

export function Card({ interactive = false, className = '', ...props }: CardProps) {
  return (
    <div
      className={`rounded-xl border border-border bg-surface shadow-card ${
        interactive ? 'transition hover:-translate-y-0.5 hover:shadow-hover' : ''
      } ${className}`}
      {...props}
    />
  );
}
