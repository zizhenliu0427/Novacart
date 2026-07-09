import type { Metadata, Viewport } from 'next';
import { Inter } from 'next/font/google';
import './globals.css';
import { CartIcon, PackageIcon, GridIcon, SparkIcon } from '@/components/icons';

const inter = Inter({
  subsets: ['latin'],
  variable: '--font-sans',
  display: 'swap',
});

export const metadata: Metadata = {
  title: 'Novacart — Modern E-Commerce',
  description: 'A modern, mobile-first e-commerce platform.',
};

// Theme-color follows the OS (light/dark).
export const viewport: Viewport = {
  themeColor: [
    { media: '(prefers-color-scheme: light)', color: '#ffffff' },
    { media: '(prefers-color-scheme: dark)', color: '#0b0e14' },
  ],
};

const navLinks = [
  { href: '/products', label: 'Products', Icon: GridIcon },
  { href: '/orders', label: 'Orders', Icon: PackageIcon },
];

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="en" className={inter.variable}>
      <body className="min-h-screen">
        <header className="sticky top-0 z-40 border-b border-border bg-surface/85 backdrop-blur">
          <div className="mx-auto flex max-w-content items-center justify-between px-4 py-3 sm:px-6">
            <a href="/" className="flex items-center gap-2 font-semibold tracking-tight text-ink">
              <span className="grid h-8 w-8 place-items-center rounded-lg bg-accent text-accent-contrast">
                <SparkIcon className="h-4 w-4" />
              </span>
              <span className="text-lg">Novacart</span>
            </a>

            <nav className="flex items-center gap-1 text-sm">
              {navLinks.map(({ href, label, Icon }) => (
                <a
                  key={href}
                  href={href}
                  className="flex items-center gap-1.5 rounded-lg px-3 py-2 text-ink-muted transition hover:bg-bg-subtle hover:text-ink"
                >
                  <Icon className="h-[18px] w-[18px]" />
                  <span className="hidden sm:inline">{label}</span>
                </a>
              ))}
              <a
                href="/cart"
                aria-label="Cart"
                className="ml-1 flex items-center gap-1.5 rounded-lg px-3 py-2 text-ink-muted transition hover:bg-bg-subtle hover:text-ink"
              >
                <CartIcon className="h-[18px] w-[18px]" />
                <span className="hidden sm:inline">Cart</span>
              </a>
            </nav>
          </div>
        </header>

        <main className="mx-auto max-w-content px-4 py-8 sm:px-6">{children}</main>

        <footer className="mt-16 border-t border-border">
          <div className="mx-auto max-w-content px-4 py-8 text-sm text-ink-muted sm:px-6">
            © {new Date().getFullYear()} Novacart · A modern e-commerce demo.
          </div>
        </footer>
      </body>
    </html>
  );
}
