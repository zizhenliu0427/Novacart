import type { Metadata, Viewport } from 'next';
import { Inter } from 'next/font/google';
import './globals.css';
import { AuthProvider } from '@/contexts/AuthContext';
import { CartProvider } from '@/contexts/CartContext';
import { WishlistProvider } from '@/contexts/WishlistContext';
import { HeaderNav } from '@/components/HeaderNav';

const inter = Inter({
  subsets: ['latin'],
  variable: '--font-sans',
  display: 'swap',
});

export const metadata: Metadata = {
  title: 'Novacart — Modern E-Commerce',
  description: 'A modern, mobile-first e-commerce platform.',
  manifest: '/manifest.json',
};

// Theme-color follows the OS (light/dark).
export const viewport: Viewport = {
  themeColor: [
    { media: '(prefers-color-scheme: light)', color: '#ffffff' },
    { media: '(prefers-color-scheme: dark)', color: '#0b0e14' },
  ],
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="en" className={inter.variable}>
      <body className="min-h-screen">
        <AuthProvider>
          <CartProvider>
            <WishlistProvider>
              <HeaderNav />

              <main className="mx-auto max-w-content px-4 py-8 sm:px-6">{children}</main>

              <footer className="mt-16 border-t border-border">
                <div className="mx-auto max-w-content px-4 py-8 text-sm text-ink-muted sm:px-6">
                  © {new Date().getFullYear()} Novacart · A modern e-commerce demo.
                </div>
              </footer>
            </WishlistProvider>
          </CartProvider>
        </AuthProvider>
        <script dangerouslySetInnerHTML={{ __html: `
          if ('serviceWorker' in navigator) {
            window.addEventListener('load', function() {
              navigator.serviceWorker.register('/sw.js').catch(function() {});
            });
          }
        ` }} />
      </body>
    </html>
  );
}
