'use client';

import Link from 'next/link';
import { useAuth } from '@/contexts/AuthContext';
import { Card } from '@/components/ui/Card';
import { Input } from '@/components/ui/Input';
import { Button } from '@/components/ui/Button';
import { EmptyState } from '@/components/ui/EmptyState';
import { GridIcon } from '@/components/icons';

/** P2-2 (Customer profile) — SCAFFOLD. Shows the current profile read-only; editing is pending. */
export default function AccountPage() {
  const { user, isLoading } = useAuth();

  if (isLoading) return <p className="text-sm text-ink-muted">Loading…</p>;

  if (!user) {
    return (
      <div className="space-y-6">
        <h1 className="text-2xl font-semibold tracking-tight text-ink">Account</h1>
        <EmptyState
          icon={<GridIcon />}
          title="Sign in to manage your account"
          action={
            <Link href="/login">
              <Button>Sign in</Button>
            </Link>
          }
        />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-3">
        <h1 className="text-2xl font-semibold tracking-tight text-ink">Account</h1>
        <span className="rounded-full bg-accent-weak px-2.5 py-0.5 text-xs font-medium text-accent">P2-2</span>
      </div>

      <Card className="max-w-md space-y-4 p-6">
        <Input label="Full name" defaultValue={user.fullName} disabled />
        <Input label="Email" defaultValue={user.email} disabled />
        <Input label="Roles" defaultValue={user.roles.join(', ')} disabled />
        <Button disabled title="Editing arrives in P2-2">Save changes</Button>
        <p className="text-xs text-ink-muted">
          Profile editing (name, password) is scaffolded — wire it to <code>GET/PUT /api/users/me</code> (P2-2).
        </p>
      </Card>
    </div>
  );
}
