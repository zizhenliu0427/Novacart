'use client';

import { useTranslations } from 'next-intl';
import { Link } from '@/i18n/navigation';
import { EmptyState } from '@/components/ui/EmptyState';
import { Button } from '@/components/ui/Button';
import { GridIcon } from '@/components/icons';

export default function NotFound() {
  const t = useTranslations('errors');

  return (
    <div className="py-8">
      <EmptyState
        icon={<GridIcon />}
        title={t('notFoundTitle')}
        description={t('notFoundDescription')}
        action={
          <Link href="/">
            <Button>{t('backHome')}</Button>
          </Link>
        }
      />
    </div>
  );
}
