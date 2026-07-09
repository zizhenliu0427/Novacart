'use client';

import { EmptyState } from '@/components/ui/EmptyState';
import { Button } from '@/components/ui/Button';
import { GridIcon } from '@/components/icons';

export default function Error({ error, reset }: { error: Error; reset: () => void }) {
  return (
    <div className="py-8">
      <EmptyState
        icon={<GridIcon />}
        title="Something went wrong"
        description={error.message || 'An unexpected error occurred. Please try again.'}
        action={
          <Button onClick={reset}>Try again</Button>
        }
      />
    </div>
  );
}
