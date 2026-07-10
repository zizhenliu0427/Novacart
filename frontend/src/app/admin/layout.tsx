'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { useAuth } from '@/contexts/AuthContext';
import { EmptyState } from '@/components/ui/EmptyState';
import { GridIcon } from '@/components/icons';

/** P2-8 admin shell. Role gating is client-side here; route auth is enforced in middleware. */
const links = [
  { href: '/admin', label: 'Dashboard' },
  { href: '/admin/products', label: 'Products' },
  { href: '/admin/orders', label: 'Orders' },
  { href: '/admin/pricing', label: 'Pricing rules' },
  { href: '/admin/analytics', label: 'Analytics' },
];

export default function AdminLayout({ children }: { children: React.ReactNode }) {
  const { user, isLoading } = useAuth();
  const pathname = usePathname();

  if (isLoading) {
    return <p className="text-sm text-ink-muted">Loading…</p>;
  }

  const isAdmin = !!user?.roles?.some((r) => r === 'admin' || r === 'sysadmin');
  if (!isAdmin) {
    return (
      <EmptyState
        icon={<GridIcon />}
        title="Admin access required"
        description="You need an administrator account to view this area."
      />
    );
  }

  return (
    <div className="grid gap-8 lg:grid-cols-[180px_1fr]">
      <aside>
        <h2 className="mb-3 px-3 text-xs font-semibold uppercase tracking-wide text-ink-muted">
          Admin
        </h2>
        <nav className="flex flex-col gap-1 text-sm">
          {links.map((l) => {
            const active = l.href === '/admin' ? pathname === '/admin' : pathname.startsWith(l.href);
            return (
              <Link
                key={l.href}
                href={l.href}
                className={`rounded-lg px-3 py-2 transition ${
                  active ? 'bg-accent-weak font-medium text-accent' : 'text-ink-muted hover:bg-bg-subtle hover:text-ink'
                }`}
              >
                {l.label}
              </Link>
            );
          })}
        </nav>
      </aside>
      <section>{children}</section>
    </div>
  );
}
