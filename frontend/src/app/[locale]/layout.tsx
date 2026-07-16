import { NextIntlClientProvider } from 'next-intl';
import { getMessages, getTranslations, setRequestLocale } from 'next-intl/server';
import { headers } from 'next/headers';
import { notFound } from 'next/navigation';
import type { Metadata } from 'next';
import { Inter } from 'next/font/google';
import { routing } from '@/i18n/routing';
import { buildHreflangAlternates } from '@/lib/seo';
import { CurrencyProvider } from '@/contexts/CurrencyContext';
import { AuthProvider } from '@/contexts/AuthContext';
import { CartProvider } from '@/contexts/CartContext';
import { WishlistProvider } from '@/contexts/WishlistContext';
import { ToastProvider } from '@/contexts/ToastContext';
import { HeaderNav } from '@/components/HeaderNav';
import { Footer } from '@/components/Footer';
import { ChatWidget } from '@/components/ChatWidget';
import '../globals.css';

const inter = Inter({
  subsets: ['latin'],
  variable: '--font-sans',
  display: 'swap',
});

export function generateStaticParams() {
  return routing.locales.map((locale) => ({ locale }));
}

export async function generateMetadata({
  params: { locale },
}: {
  params: { locale: string };
}): Promise<Metadata> {
  const t = await getTranslations({ locale, namespace: 'home' });
  const path = headers().get('x-next-pathname') ?? '/';
  const alternates = buildHreflangAlternates(path, locale);

  return {
    title: {
      default: t('metaTitle'),
      template: `%s · Novacart`,
    },
    description: t('metaDescription'),
    alternates,
  };
}

export default async function LocaleLayout({
  children,
  params: { locale },
}: {
  children: React.ReactNode;
  params: { locale: string };
}) {
  if (!routing.locales.includes(locale as typeof routing.locales[number])) {
    notFound();
  }

  setRequestLocale(locale);
  const messages = await getMessages();

  return (
    <html lang={locale === 'zh' ? 'zh-CN' : 'en-AU'} className={inter.variable}>
      <body className="min-h-screen">
        <NextIntlClientProvider messages={messages}>
          <CurrencyProvider>
            <AuthProvider>
              <CartProvider>
                <WishlistProvider>
                  <ToastProvider>
                    <HeaderNav />
                    <main className="mx-auto max-w-content px-4 py-8 sm:px-6">{children}</main>
                    <footer className="mt-16 border-t border-border">
                      <Footer />
                    </footer>
                    <ChatWidget />
                  </ToastProvider>
                </WishlistProvider>
              </CartProvider>
            </AuthProvider>
          </CurrencyProvider>
        </NextIntlClientProvider>
        <script
          dangerouslySetInnerHTML={{
            __html: `
          if ('serviceWorker' in navigator) {
            window.addEventListener('load', function() {
              navigator.serviceWorker.register('/sw.js').catch(function() {});
            });
          }
        `,
          }}
        />
      </body>
    </html>
  );
}
