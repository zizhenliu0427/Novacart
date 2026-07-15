'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { useState } from 'react';
import { useAuth } from '@/contexts/AuthContext';
import { EmptyState } from '@/components/ui/EmptyState';
import { GridIcon, MenuIcon, CloseIcon } from '@/components/icons';

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
  const [sidebarOpen, setSidebarOpen] = useState(false);

  if (isLoading) {
    return <p className="text-sm text-ink-muted">Loading…</p>;
  }

  const isAdmin = !!user?.roles?.some((r) => r === 'admin' || r === 'sysadmin');
  const isSysAdmin = !!user?.roles?.some((r) => r === 'sysadmin');

  if (!isAdmin) {
    return (
      <EmptyState
        icon={<GridIcon />}
        title="Admin access required"
        description="You need an administrator account to view this area."
      />
    );
  }

  const visibleLinks = isSysAdmin
    ? [...links, { href: '/admin/system', label: 'System' }]
    : links;

  const navContent = (
    <nav className="flex flex-col gap-1 text-sm">
      {visibleLinks.map((l) => {
        const active = l.href === '/admin' ? pathname === '/admin' : pathname.startsWith(l.href);
        return (
          <Link
            key={l.href}
            href={l.href}
            onClick={() => setSidebarOpen(false)}
            className={`rounded-lg px-3 py-2 transition ${
              active ? 'bg-accent-weak font-medium text-accent' : 'text-ink-muted hover:bg-bg-subtle hover:text-ink'
            }`}
          >
            {l.label}
          </Link>
        );
      })}
    </nav>
  );

  return (
    <div className="grid gap-8 md:grid-cols-[180px_1fr]">
      {/* Desktop sidebar — hidden on mobile */}
      <aside className="hidden md:block">
        <h2 className="mb-3 px-3 text-xs font-semibold uppercase tracking-wide text-ink-muted">
          Admin
        </h2>
        {navContent}
      </aside>

      {/* Mobile sidebar toggle */}
      <div className="flex items-center gap-2 md:hidden">
        <button
          id="admin-sidebar-toggle"
          onClick={() => setSidebarOpen((o) => !o)}
          aria-expanded={sidebarOpen}
          aria-label={sidebarOpen ? 'Close admin menu' : 'Open admin menu'}
          className="rounded-lg border border-border p-2 text-ink-muted transition hover:bg-bg-subtle hover:text-ink"
        >
          {sidebarOpen ? <CloseIcon className="h-4 w-4" /> : <MenuIcon className="h-4 w-4" />}
        </button>
        <span className="text-xs font-semibold uppercase tracking-wide text-ink-muted">Admin</span>
      </div>

      {/* Mobile sidebar panel (slide-down) */}
      {sidebarOpen && (
        <div className="rounded-lg border border-border bg-surface p-3 md:hidden">
          {navContent}
        </div>
      )}

      <section>{children}</section>
    </div>
  );
}
