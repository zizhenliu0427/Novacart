import { Card } from './Card';

/** Scaffold placeholder for pages whose implementation is a pending P2 item. */
export function ComingSoon({ title, item, children }: { title: string; item: string; children?: React.ReactNode }) {
  return (
    <div className="space-y-4">
      <div className="flex items-center gap-3">
        <h1 className="text-2xl font-semibold tracking-tight text-ink">{title}</h1>
        <span className="rounded-full bg-accent-weak px-2.5 py-0.5 text-xs font-medium text-accent">{item}</span>
      </div>
      <Card className="p-6 text-sm text-ink-muted">
        <p className="font-medium text-ink">Scaffolded — implementation pending.</p>
        <p className="mt-1">This screen is wired into routing and the design system; the feature body is tracked as <strong>{item}</strong> in the handoff roadmap.</p>
        {children && <div className="mt-4">{children}</div>}
      </Card>
    </div>
  );
}
