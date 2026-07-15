'use client';

import { useState } from 'react';
import Link from 'next/link';
import { useCart } from '@/contexts/CartContext';
import { useAuth } from '@/contexts/AuthContext';
import { Button } from '@/components/ui/Button';
import { Card } from '@/components/ui/Card';
import { EmptyState } from '@/components/ui/EmptyState';
import { CartIcon, TrashIcon, PlusIcon, MinusIcon } from '@/components/icons';
import { formatPrice } from '@/types/product';


import { useRouter } from 'next/navigation';

export default function CartPage() {
  const { user } = useAuth();
  const { cart, isLoading, updateItem, removeItem } = useCart();
  const router = useRouter();
  const [busy, setBusy] = useState<string | null>(null); // cartItemId being modified

  function handleCheckout() {
    if (!user) {
      router.push('/login?redirect=/checkout');
      return;
    }
    router.push('/checkout');
  }

  if (!user && !isLoading && (!cart || cart.items.length === 0)) {
    return (
      <div className="space-y-6">
        <h1 className="text-2xl font-semibold tracking-tight text-ink">Your cart</h1>
        <EmptyState
          icon={<CartIcon />}
          title="Your cart is empty"
          description="Browse our products and add items to your cart."
          action={
            <Link href="/products">
              <Button>Browse Products</Button>
            </Link>
          }
        />
        <p className="text-center text-sm text-ink-muted">
          <Link href="/login" className="text-accent hover:underline">Sign in</Link>{' '}
          to save your cart across devices.
        </p>
      </div>
    );
  }

  if (isLoading) {
    return (
      <div className="space-y-6">
        <h1 className="text-2xl font-semibold tracking-tight text-ink">Your cart</h1>
        <div className="space-y-3">
          {Array.from({ length: 3 }).map((_, i) => (
            <div key={i} className="h-24 animate-pulse rounded-xl bg-bg-subtle" />
          ))}
        </div>
      </div>
    );
  }

  const items = cart?.items ?? [];

  if (items.length === 0) {
    return (
      <div className="space-y-6">
        <h1 className="text-2xl font-semibold tracking-tight text-ink">Your cart</h1>
        <EmptyState
          icon={<CartIcon />}
          title="Your cart is empty"
          description="Browse the catalogue and add items — they'll show up here."
          action={
            <Link href="/products">
              <Button>Browse products</Button>
            </Link>
          }
        />
      </div>
    );
  }

  async function handleUpdate(cartItemId: string, quantity: number) {
    setBusy(cartItemId);
    try { await updateItem(cartItemId, quantity); }
    finally { setBusy(null); }
  }

  async function handleRemove(cartItemId: string) {
    setBusy(cartItemId);
    try { await removeItem(cartItemId); }
    finally { setBusy(null); }
  }

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-semibold tracking-tight text-ink">Your cart</h1>

      <div className="grid gap-6 lg:grid-cols-3">
        {/* Item list */}
        <div className="space-y-3 lg:col-span-2">
          {items.map((item) => {
            const isBusy = busy === item.id;
            return (
              <Card key={item.id} className="flex items-start gap-4 p-4">
                {/* Product image placeholder */}
                <div className="flex h-20 w-20 shrink-0 items-center justify-center rounded-lg bg-bg-subtle">
                  <span className="text-2xl font-semibold text-ink-muted/40">
                    {item.productName.charAt(0)}
                  </span>
                </div>

                {/* Details */}
                <div className="flex flex-1 flex-col gap-2">
                  <div className="flex items-start justify-between gap-4">
                    <Link
                      href={`/products/${item.productId}`}
                      className="font-semibold leading-snug text-ink hover:text-accent"
                    >
                      {item.productName}
                    </Link>
                    <span className="tnum shrink-0 font-semibold text-ink">
                      {formatPrice(item.lineTotal)}
                    </span>
                  </div>

                  <span className="tnum text-sm text-ink-muted">
                    {formatPrice(item.unitPrice)} each
                  </span>

                  {/* Quantity stepper + remove */}
                  <div className="flex items-center gap-3">
                    <div className="flex items-center rounded-lg border border-border">
                      <button
                        aria-label="Decrease quantity"
                        disabled={isBusy || item.quantity <= 1}
                        onClick={() => handleUpdate(item.id, item.quantity - 1)}
                        className="flex h-8 w-8 items-center justify-center text-ink-muted transition hover:text-ink disabled:opacity-40"
                      >
                        <MinusIcon className="h-4 w-4" />
                      </button>
                      <span className="tnum w-8 text-center text-sm font-medium text-ink">
                        {item.quantity}
                      </span>
                      <button
                        aria-label="Increase quantity"
                        disabled={isBusy || item.quantity >= item.stockQuantity}
                        onClick={() => handleUpdate(item.id, item.quantity + 1)}
                        className="flex h-8 w-8 items-center justify-center text-ink-muted transition hover:text-ink disabled:opacity-40"
                      >
                        <PlusIcon className="h-4 w-4" />
                      </button>
                    </div>

                    <button
                      aria-label={`Remove ${item.productName} from cart`}
                      disabled={isBusy}
                      onClick={() => handleRemove(item.id)}
                      className="flex items-center gap-1 text-sm text-ink-muted transition hover:text-danger disabled:opacity-40"
                    >
                      <TrashIcon className="h-4 w-4" />
                      <span className="hidden sm:inline">Remove</span>
                    </button>
                  </div>

                  {/* Low stock warning */}
                  {item.stockQuantity > 0 && item.stockQuantity <= 5 && (
                    <p className="text-xs text-warning">
                      Only {item.stockQuantity} left in stock
                    </p>
                  )}
                </div>
              </Card>
            );
          })}
        </div>

        {/* Order summary */}
        <div className="lg:col-span-1">
          <Card className="p-5">
            <h2 className="mb-4 font-semibold text-ink">Order summary</h2>
            <dl className="space-y-2 text-sm">
              <div className="flex justify-between">
                <dt className="text-ink-muted">
                  Subtotal ({cart?.totalItems} item{cart?.totalItems !== 1 ? 's' : ''})
                </dt>
                <dd className="tnum font-medium text-ink">
                  {formatPrice(cart?.subtotal ?? 0)}
                </dd>
              </div>
              <div className="flex justify-between">
                <dt className="text-ink-muted">Shipping</dt>
                <dd className="text-ink-muted">Calculated at checkout</dd>
              </div>
            </dl>

            <div className="my-4 border-t border-border" />

            <div className="flex justify-between text-base font-semibold text-ink">
              <span>Total</span>
              <span className="tnum">{formatPrice(cart?.subtotal ?? 0)}</span>
            </div>

            <Button
              className="mt-4 w-full"
              onClick={handleCheckout}
            >
              Proceed to checkout
            </Button>
            <Link href="/products" className="mt-2 block">
              <Button variant="ghost" className="w-full text-sm">
                Continue shopping
              </Button>
            </Link>
          </Card>
        </div>
      </div>
    </div>
  );
}
