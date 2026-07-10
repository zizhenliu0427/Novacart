'use client';

import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { useState } from 'react';
import { useAuth } from '@/contexts/AuthContext';
import { useCart } from '@/contexts/CartContext';
import { CartIcon, PackageIcon, GridIcon, SparkIcon, UserIcon, MenuIcon, CloseIcon } from '@/components/icons';

const navLinks = [
  { href: '/products', label: 'Products', Icon: GridIcon },
  { href: '/orders', label: 'Orders', Icon: PackageIcon },
];

export function HeaderNav() {
  const { user, isLoading, logout } = useAuth();
  const { totalItems } = useCart();
  const router = useRouter();
  const [menuOpen, setMenuOpen] = useState(false);
  const [mobileOpen, setMobileOpen] = useState(false);

  async function handleLogout() {
    setMenuOpen(false);
    setMobileOpen(false);
    await logout();
    router.push('/');
  }

  return (
    <header className="sticky top-0 z-40 border-b border-border bg-surface/85 backdrop-blur">
      <div className="mx-auto flex max-w-content items-center justify-between px-4 py-3 sm:px-6">
        {/* Wordmark */}
        <Link href="/" className="flex items-center gap-2 font-semibold tracking-tight text-ink">
          <span className="grid h-8 w-8 place-items-center rounded-lg bg-accent text-accent-contrast">
            <SparkIcon className="h-4 w-4" />
          </span>
          <span className="text-lg">Novacart</span>
        </Link>

        {/* Desktop nav */}
        <nav className="hidden items-center gap-1 text-sm sm:flex">
          {navLinks.map(({ href, label, Icon }) => (
            <Link
              key={href}
              href={href}
              className="flex items-center gap-1.5 rounded-lg px-3 py-2 text-ink-muted transition hover:bg-bg-subtle hover:text-ink"
            >
              <Icon className="h-[18px] w-[18px]" />
              <span>{label}</span>
            </Link>
          ))}

          {/* Cart */}
          <Link
            href="/cart"
            aria-label={`Cart${totalItems > 0 ? `, ${totalItems} item${totalItems !== 1 ? 's' : ''}` : ''}`}
            className="relative ml-1 flex items-center gap-1.5 rounded-lg px-3 py-2 text-ink-muted transition hover:bg-bg-subtle hover:text-ink"
          >
            <CartIcon className="h-[18px] w-[18px]" />
            <span>Cart</span>
            {totalItems > 0 && (
              <span className="absolute -right-1 -top-1 flex h-5 w-5 items-center justify-center rounded-full bg-accent text-[10px] font-bold text-accent-contrast">
                {totalItems > 99 ? '99+' : totalItems}
              </span>
            )}
          </Link>

          {/* Auth area (desktop) */}
          {!isLoading && (
            <>
              {user ? (
                <div className="relative ml-1">
                  <button
                    id="user-menu-button"
                    onClick={() => setMenuOpen((o) => !o)}
                    aria-expanded={menuOpen}
                    aria-haspopup="true"
                    className="flex items-center gap-2 rounded-lg px-3 py-2 text-sm text-ink-muted transition hover:bg-bg-subtle hover:text-ink"
                  >
                    <span className="grid h-7 w-7 place-items-center rounded-full bg-accent-weak text-accent">
                      <UserIcon className="h-4 w-4" />
                    </span>
                    <span className="max-w-[120px] truncate">{user.fullName.split(' ')[0]}</span>
                  </button>

                  {menuOpen && (
                    <>
                      {/* Backdrop */}
                      <div
                        className="fixed inset-0 z-40"
                        onClick={() => setMenuOpen(false)}
                        aria-hidden="true"
                      />
                      <div
                        role="menu"
                        className="absolute right-0 z-50 mt-1 w-52 rounded-xl border border-border bg-surface p-1 shadow-hover"
                      >
                        <div className="px-3 py-2 text-sm">
                          <p className="font-medium text-ink">{user.fullName}</p>
                          <p className="truncate text-xs text-ink-muted">{user.email}</p>
                        </div>
                        <div className="my-1 border-t border-border" />
                        {[
                          { href: '/account', label: 'Account' },
                          { href: '/wishlist', label: 'Wishlist' },
                          { href: '/orders', label: 'Orders' },
                        ].map((l) => (
                          <Link
                            key={l.href}
                            href={l.href}
                            role="menuitem"
                            onClick={() => setMenuOpen(false)}
                            className="block rounded-lg px-3 py-2 text-sm text-ink-muted transition hover:bg-bg-subtle hover:text-ink"
                          >
                            {l.label}
                          </Link>
                        ))}
                        {user.roles?.some((r) => r === 'admin' || r === 'sysadmin') && (
                          <Link
                            href="/admin"
                            role="menuitem"
                            onClick={() => setMenuOpen(false)}
                            className="block rounded-lg px-3 py-2 text-sm font-medium text-accent transition hover:bg-accent-weak"
                          >
                            Admin dashboard
                          </Link>
                        )}
                        <div className="my-1 border-t border-border" />
                        <button
                          id="logout-button"
                          role="menuitem"
                          onClick={handleLogout}
                          className="w-full rounded-lg px-3 py-2 text-left text-sm text-ink-muted transition hover:bg-bg-subtle hover:text-ink"
                        >
                          Sign out
                        </button>
                      </div>
                    </>
                  )}
                </div>
              ) : (
                <Link
                  href="/login"
                  id="header-sign-in"
                  className="ml-1 flex items-center gap-1.5 rounded-lg px-3 py-2 text-sm text-ink-muted transition hover:bg-bg-subtle hover:text-ink"
                >
                  <UserIcon className="h-[18px] w-[18px]" />
                  <span>Sign in</span>
                </Link>
              )}
            </>
          )}
        </nav>

        {/* Mobile: cart badge + hamburger */}
        <div className="flex items-center gap-1 sm:hidden">
          {/* Mobile cart */}
          <Link
            href="/cart"
            aria-label={`Cart${totalItems > 0 ? `, ${totalItems} items` : ''}`}
            className="relative rounded-lg p-2 text-ink-muted transition hover:bg-bg-subtle hover:text-ink"
          >
            <CartIcon className="h-5 w-5" />
            {totalItems > 0 && (
              <span className="absolute -right-0.5 -top-0.5 flex h-4 w-4 items-center justify-center rounded-full bg-accent text-[9px] font-bold text-accent-contrast">
                {totalItems > 99 ? '99+' : totalItems}
              </span>
            )}
          </Link>
          <button
            id="mobile-menu-button"
            onClick={() => setMobileOpen((o) => !o)}
            aria-expanded={mobileOpen}
            aria-label={mobileOpen ? 'Close menu' : 'Open menu'}
            className="rounded-lg p-2 text-ink-muted transition hover:bg-bg-subtle hover:text-ink"
          >
            {mobileOpen ? <CloseIcon className="h-5 w-5" /> : <MenuIcon className="h-5 w-5" />}
          </button>
        </div>
      </div>

      {/* Mobile slide-down menu */}
      {mobileOpen && (
        <nav className="border-t border-border bg-surface px-4 pb-4 pt-2 sm:hidden">
          <div className="flex flex-col gap-1 text-sm">
            {navLinks.map(({ href, label, Icon }) => (
              <Link
                key={href}
                href={href}
                onClick={() => setMobileOpen(false)}
                className="flex items-center gap-2.5 rounded-lg px-3 py-2.5 text-ink-muted transition hover:bg-bg-subtle hover:text-ink"
              >
                <Icon className="h-[18px] w-[18px]" />
                {label}
              </Link>
            ))}

            {!isLoading && user && (
              <>
                <div className="my-1 border-t border-border" />
                {[
                  { href: '/account', label: 'Account' },
                  { href: '/wishlist', label: 'Wishlist' },
                ].map((l) => (
                  <Link
                    key={l.href}
                    href={l.href}
                    onClick={() => setMobileOpen(false)}
                    className="flex items-center gap-2.5 rounded-lg px-3 py-2.5 text-ink-muted transition hover:bg-bg-subtle hover:text-ink"
                  >
                    {l.label}
                  </Link>
                ))}
                {user.roles?.some((r) => r === 'admin' || r === 'sysadmin') && (
                  <Link
                    href="/admin"
                    onClick={() => setMobileOpen(false)}
                    className="flex items-center gap-2.5 rounded-lg px-3 py-2.5 font-medium text-accent transition hover:bg-accent-weak"
                  >
                    Admin dashboard
                  </Link>
                )}
                <div className="my-1 border-t border-border" />
                <button
                  onClick={handleLogout}
                  className="w-full rounded-lg px-3 py-2.5 text-left text-sm text-ink-muted transition hover:bg-bg-subtle hover:text-ink"
                >
                  Sign out
                </button>
              </>
            )}

            {!isLoading && !user && (
              <>
                <div className="my-1 border-t border-border" />
                <Link
                  href="/login"
                  onClick={() => setMobileOpen(false)}
                  className="flex items-center gap-2.5 rounded-lg px-3 py-2.5 text-ink-muted transition hover:bg-bg-subtle hover:text-ink"
                >
                  <UserIcon className="h-[18px] w-[18px]" />
                  Sign in
                </Link>
              </>
            )}
          </div>
        </nav>
      )}
    </header>
  );
}
