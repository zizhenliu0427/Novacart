import type { Metadata } from 'next';
import './globals.css';

export const metadata: Metadata = {
  title: 'Novacart — Modern E-Commerce',
  description: 'A modern e-commerce platform built with Next.js and ASP.NET Core',
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="en">
      <body className="min-h-screen bg-gray-50 text-gray-900">
        <header className="border-b bg-white">
          <div className="mx-auto max-w-7xl px-4 py-4 flex items-center justify-between">
            <a href="/" className="text-2xl font-bold text-indigo-600">
              Novacart
            </a>
            <nav className="flex gap-6 text-sm">
              <a href="/products" className="hover:text-indigo-600">Products</a>
              <a href="/cart" className="hover:text-indigo-600">Cart</a>
              <a href="/orders" className="hover:text-indigo-600">Orders</a>
            </nav>
          </div>
        </header>
        <main className="mx-auto max-w-7xl px-4 py-8">{children}</main>
      </body>
    </html>
  );
}
