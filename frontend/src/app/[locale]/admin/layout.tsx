'use client';

import { useTranslations } from 'next-intl';
import { Link, usePathname } from '@/i18n/navigation';
import { useState } from 'react';
import { useAuth } from '@/contexts/AuthContext';
import { EmptyState } from '@/components/ui/EmptyState';
import { GridIcon, MenuIcon, CloseIcon } from '@/components/icons';
import { CurrencySwitcher } from '@/components/CurrencySwitcher';
import { CurrencyDisclaimer } from '@/components/CurrencyDisclaimer';

export default function AdminLayout({ children }: { children: React.ReactNode }) {
  const t = useTranslations('admin');
  const { user, isLoading } = useAuth();
  const pathname = usePathname();
  const [sidebarOpen, setSidebarOpen] = useState(false);

  const links = [
    { href: '/admin' as const, label: t('dashboard') },
    { href: '/admin/products' as const, label: t('products') },
    { href: '/admin/orders' as const, label: t('orders') },
    { href: '/admin/pricing' as const, label: t('pricingRules') },
    { href: '/admin/analytics' as const, label: t('analytics') },
  ];

  if (isLoading) {
    return <p className="text-sm text-ink-muted">{t('loading')}</p>;
  }

  const isAdmin = !!user?.roles?.some((r) => r === 'admin' || r === 'sysadmin');
  const isSysAdmin = !!user?.roles?.some((r) => r === 'sysadmin');

  if (!isAdmin) {
    return (
      <EmptyState
        icon={<GridIcon />}
        title={t('accessRequired')}
        description={t('accessDescription')}
      />
    );
  }

  const visibleLinks = isSysAdmin
    ? [...links, { href: '/admin/system' as const, label: t('system') }]
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
      <aside className="hidden md:block">
        <h2 className="mb-3 px-3 text-xs font-semibold uppercase tracking-wide text-ink-muted">
          {t('title')}
        </h2>
        <div className="mb-4 px-3 space-y-2">
          <CurrencySwitcher />
          <CurrencyDisclaimer variant="admin" />
        </div>
        {navContent}
      </aside>

      <div className="flex items-center justify-between gap-2 md:hidden">
        <div className="flex items-center gap-2">
          <button
            id="admin-sidebar-toggle"
            onClick={() => setSidebarOpen((o) => !o)}
            aria-expanded={sidebarOpen}
            aria-label={sidebarOpen ? t('closeMenu') : t('menu')}
            className="rounded-lg border border-border p-2 text-ink-muted transition hover:bg-bg-subtle hover:text-ink"
          >
            {sidebarOpen ? <CloseIcon className="h-4 w-4" /> : <MenuIcon className="h-4 w-4" />}
          </button>
          <span className="text-xs font-semibold uppercase tracking-wide text-ink-muted">{t('title')}</span>
        </div>
        <CurrencySwitcher />
      </div>

      {sidebarOpen && (
        <div className="rounded-lg border border-border bg-surface p-3 md:hidden">
          {navContent}
        </div>
      )}

      <section>{children}</section>
    </div>
  );
}
