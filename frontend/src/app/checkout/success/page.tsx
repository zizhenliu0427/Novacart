'use client';

import { useSearchParams } from 'next/navigation';
import { Button } from '@/components/ui/Button';
import { Card } from '@/components/ui/Card';

export default function CheckoutSuccessPage() {
  const searchParams = useSearchParams();
  const sessionId = searchParams.get('session_id');

  return (
    <div className="flex flex-col items-center justify-center min-h-[60vh] py-12 px-4">
      <Card className="max-w-md w-full text-center p-8 space-y-6 shadow-md border border-border">
        {/* Success Checkmark Indicator */}
        <div className="mx-auto flex items-center justify-center h-16 w-16 rounded-full bg-green-50 dark:bg-green-950/20 text-green-600">
          <svg className="h-10 w-10" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M5 13l4 4L19 7" />
          </svg>
        </div>

        <div className="space-y-2">
          <h1 className="text-2xl font-semibold tracking-tight text-ink">Payment Successful!</h1>
          <p className="text-sm text-ink-muted">
            Thank you for your purchase. Your payment has been processed and your order is confirmed.
          </p>
        </div>

        {sessionId && (
          <div className="bg-bg-subtle p-3 rounded-lg border border-border text-left">
            <span className="text-xs font-medium text-ink-muted uppercase block">Transaction Reference</span>
            <code className="text-xs text-ink font-mono break-all">{sessionId}</code>
          </div>
        )}

        <div className="pt-4 flex flex-col sm:flex-row gap-3 justify-center">
          <a href="/orders" className="w-full sm:w-auto">
            <Button className="w-full">View Order History</Button>
          </a>
          <a href="/products" className="w-full sm:w-auto">
            <Button variant="secondary" className="w-full">Continue Shopping</Button>
          </a>
        </div>
      </Card>
    </div>
  );
}
