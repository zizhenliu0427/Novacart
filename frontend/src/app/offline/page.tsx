'use client';

import Link from 'next/link';
import { Button } from '@/components/ui/Button';
import { Card } from '@/components/ui/Card';

export default function OfflinePage() {
  return (
    <div className="flex flex-col items-center justify-center min-h-[70vh] py-12 px-4">
      <Card className="max-w-md w-full text-center p-8 space-y-6 shadow-md border border-border">
        {/* Connection Icon */}
        <div className="mx-auto flex items-center justify-center h-16 w-16 rounded-full bg-bg-subtle text-ink-muted">
          <svg className="h-10 w-10" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M18.364 5.636a9 9 0 010 12.728m0 0l-2.829-2.829m2.829 2.829L21 21M21 3L3 21M8.464 8.464a5 5 0 017.072 0M12 12a1 1 0 100 2 1 1 0 000-2z" />
          </svg>
        </div>

        <div className="space-y-2">
          <h1 className="text-2xl font-semibold tracking-tight text-ink">You are offline</h1>
          <p className="text-sm text-ink-muted">
            It looks like you don&apos;t have an active internet connection right now. Some features may not be available.
          </p>
        </div>

        <div className="pt-4 flex flex-col gap-3 justify-center">
          <Button onClick={() => window.location.reload()} className="w-full">
            Retry Connection
          </Button>
          <Link href="/" className="w-full">
            <Button variant="secondary" className="w-full">Go to Homepage</Button>
          </Link>
        </div>
      </Card>
    </div>
  );
}
