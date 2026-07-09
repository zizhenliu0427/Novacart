'use client';

import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { useState } from 'react';
import { useAuth } from '@/contexts/AuthContext';
import { useCart } from '@/contexts/CartContext';
import { CartIcon, PackageIcon, GridIcon, SparkIcon, UserIcon } from '@/components/icons';

const navLinks = [
  { href: '/products', label: 'Products', Icon: GridIcon },
  { href: '/orders', label: 'Orders', Icon: PackageIcon },
];

export function HeaderNav() {
  const { user, isLoading, logout } = useAuth();
  const { totalItems } = useCart();
  const router = useRouter();
  const [menuOpen, setMenuOpen] = useState(false);

  async function handleLogout() {
    setMenuOpen(false);
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

        {/* Nav */}
        <nav className="flex items-center gap-1 text-sm">
          {navLinks.map(({ href, label, Icon }) => (
            <Link
              key={href}
              href={href}
              className="flex items-center gap-1.5 rounded-lg px-3 py-2 text-ink-muted transition hover:bg-bg-subtle hover:text-ink"
            >
              <Icon className="h-[18px] w-[18px]" />
              <span className="hidden sm:inline">{label}</span>
            </Link>
          ))}

          {/* Cart */}
          <Link
            href="/cart"
            aria-label={`Cart${totalItems > 0 ? `, ${totalItems} item${totalItems !== 1 ? 's' : ''}` : ''}`}
            className="relative ml-1 flex items-center gap-1.5 rounded-lg px-3 py-2 text-ink-muted transition hover:bg-bg-subtle hover:text-ink"
          >
            <CartIcon className="h-[18px] w-[18px]" />
            <span className="hidden sm:inline">Cart</span>
            {totalItems > 0 && (
              <span className="absolute -right-1 -top-1 flex h-5 w-5 items-center justify-center rounded-full bg-accent text-[10px] font-bold text-accent-contrast">
                {totalItems > 99 ? '99+' : totalItems}
              </span>
            )}
          </Link>

          {/* Auth area */}
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
                    <span className="hidden max-w-[120px] truncate sm:inline">{user.fullName.split(' ')[0]}</span>
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
                  <span className="hidden sm:inline">Sign in</span>
                </Link>
              )}
            </>
          )}
        </nav>
      </div>
    </header>
  );
}
