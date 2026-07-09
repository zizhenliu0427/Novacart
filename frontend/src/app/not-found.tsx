import { EmptyState } from '@/components/ui/EmptyState';
import { Button } from '@/components/ui/Button';
import { GridIcon } from '@/components/icons';

export default function NotFound() {
  return (
    <div className="py-8">
      <EmptyState
        icon={<GridIcon />}
        title="Page not found"
        description="The page you're looking for doesn't exist or may have moved."
        action={
          <a href="/">
            <Button>Back to home</Button>
          </a>
        }
      />
    </div>
  );
}
