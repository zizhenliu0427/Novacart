'use client';

import { Button } from '@/components/ui/Button';
import { Card } from '@/components/ui/Card';

export default function CheckoutCancelPage() {
  return (
    <div className="flex flex-col items-center justify-center min-h-[60vh] py-12 px-4">
      <Card className="max-w-md w-full text-center p-8 space-y-6 shadow-md border border-border">
        {/* Warning Indicator */}
        <div className="mx-auto flex items-center justify-center h-16 w-16 rounded-full bg-amber-50 dark:bg-amber-950/20 text-amber-600">
          <svg className="h-10 w-10" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
          </svg>
        </div>

        <div className="space-y-2">
          <h1 className="text-2xl font-semibold tracking-tight text-ink">Payment Cancelled</h1>
          <p className="text-sm text-ink-muted">
            The checkout session was cancelled. No charges were made, and your items are still safe in your cart.
          </p>
        </div>

        <div className="pt-4 flex flex-col sm:flex-row gap-3 justify-center">
          <a href="/cart" className="w-full sm:w-auto">
            <Button className="w-full">Return to Cart</Button>
          </a>
          <a href="/products" className="w-full sm:w-auto">
            <Button variant="secondary" className="w-full">Back to Products</Button>
          </a>
        </div>
      </Card>
    </div>
  );
}
