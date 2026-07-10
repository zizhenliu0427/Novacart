'use client';

import { FormEvent, useEffect, useState } from 'react';
import Link from 'next/link';
import { useAuth } from '@/contexts/AuthContext';
import { Card } from '@/components/ui/Card';
import { Input } from '@/components/ui/Input';
import { Button } from '@/components/ui/Button';
import { EmptyState } from '@/components/ui/EmptyState';
import { GridIcon } from '@/components/icons';
import { apiCall } from '@/lib/api';

interface UserProfile {
  id: string;
  email: string;
  fullName: string;
  roles: string[];
}

/** P2-2 (Customer profile) — edit full name via GET/PUT /api/users/me. */
export default function AccountPage() {
  const { user, token, isLoading } = useAuth();
  const [fullName, setFullName] = useState('');
  const [email, setEmail] = useState('');
  const [roles, setRoles] = useState<string[]>([]);
  const [editing, setEditing] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [loaded, setLoaded] = useState(false);

  useEffect(() => {
    if (!token || loaded) return;
    apiCall<UserProfile>('/users/me', { token })
      .then((profile) => {
        setFullName(profile.fullName);
        setEmail(profile.email);
        setRoles(profile.roles);
        setLoaded(true);
      })
      .catch((err) => {
        setError(err instanceof Error ? err.message : 'Failed to load profile.');
        setLoaded(true);
      });
  }, [token, loaded]);

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

  async function submitProfile(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!token) return;
    setError(null);
    setSaving(true);
    try {
      const updated = await apiCall<UserProfile>('/users/me', {
        method: 'PUT',
        token,
        body: { fullName },
      });
      setFullName(updated.fullName);
      setNotice('Profile updated.');
      setEditing(false);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to update profile.');
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-semibold tracking-tight text-ink">Account</h1>

      {notice && (
        <div className="flex items-center justify-between rounded-lg border border-border bg-bg-subtle px-4 py-3 text-sm text-success">
          <span>{notice}</span>
          <button onClick={() => setNotice(null)} className="text-ink-muted hover:text-ink" aria-label="Dismiss">×</button>
        </div>
      )}

      {error && (
        <div className="flex items-center justify-between rounded-lg border border-border bg-bg-subtle px-4 py-3 text-sm text-danger">
          <span>{error}</span>
          <button onClick={() => setError(null)} className="text-ink-muted hover:text-ink" aria-label="Dismiss">×</button>
        </div>
      )}

      <Card className="max-w-md p-6">
        {editing ? (
          <form onSubmit={submitProfile} className="space-y-4">
            <Input
              label="Full name"
              required
              minLength={1}
              maxLength={200}
              value={fullName}
              onChange={(event) => setFullName(event.target.value)}
            />
            <Input label="Email" defaultValue={email} disabled helperText="Email changes require verification (not yet available)." />
            <div className="flex gap-3">
              <Button type="submit" disabled={saving}>{saving ? 'Saving…' : 'Save changes'}</Button>
              <Button type="button" variant="secondary" onClick={() => setEditing(false)} disabled={saving}>Cancel</Button>
            </div>
          </form>
        ) : (
          <div className="space-y-4">
            <Input label="Full name" defaultValue={fullName} disabled />
            <Input label="Email" defaultValue={email} disabled />
            <Input label="Roles" defaultValue={roles.join(', ')} disabled />
            <Button onClick={() => setEditing(true)}>Edit profile</Button>
          </div>
        )}
      </Card>
    </div>
  );
}
