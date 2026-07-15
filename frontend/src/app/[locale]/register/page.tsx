'use client';

import { useState, type FormEvent } from 'react';
import { useTranslations } from 'next-intl';
import { Link, useRouter } from '@/i18n/navigation';
import { useAuth } from '@/contexts/AuthContext';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { SparkIcon } from '@/components/icons';

export default function RegisterPage() {
  const t = useTranslations('auth');
  const { register } = useAuth();
  const router = useRouter();

  const [fullName, setFullName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);

    if (password.length < 8) {
      setError(t('passwordMinLength'));
      return;
    }

    setLoading(true);
    try {
      await register({ fullName, email, password });
      router.push('/');
    } catch (err) {
      setError(err instanceof Error ? err.message : t('registerFailed'));
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="mx-auto w-full max-w-sm px-4 py-12">
      <div className="mb-8 flex flex-col items-center gap-3 text-center">
        <span className="grid h-12 w-12 place-items-center rounded-xl bg-accent text-accent-contrast">
          <SparkIcon className="h-6 w-6" />
        </span>
        <div>
          <h1 className="text-2xl font-semibold tracking-tight text-ink">{t('createAccount')}</h1>
          <p className="mt-1 text-sm text-ink-muted">{t('registerSubtitle')}</p>
        </div>
      </div>

      <form
        id="register-form"
        onSubmit={handleSubmit}
        className="space-y-5 rounded-xl border border-border bg-surface p-6 shadow-card"
      >
        {error && (
          <div
            role="alert"
            className="rounded-lg border border-danger/30 bg-danger/5 px-4 py-3 text-sm text-danger"
          >
            {error}
          </div>
        )}

        <Input
          id="register-name"
          label={t('fullName')}
          type="text"
          autoComplete="name"
          required
          value={fullName}
          onChange={(e) => setFullName(e.target.value)}
          placeholder="Jane Smith"
          size="lg"
        />

        <Input
          id="register-email"
          label={t('email')}
          type="email"
          autoComplete="email"
          required
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          placeholder="you@example.com"
          size="lg"
        />

        <Input
          id="register-password"
          label={t('passwordHint')}
          type="password"
          autoComplete="new-password"
          required
          minLength={8}
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          placeholder="••••••••"
          size="lg"
        />

        <Button type="submit" className="w-full" disabled={loading}>
          {loading ? t('creatingAccount') : t('createAccountButton')}
        </Button>
      </form>

      <p className="mt-5 text-center text-sm text-ink-muted">
        {t('haveAccount')}{' '}
        <Link href="/login" className="font-medium text-accent hover:underline">
          {t('signIn')}
        </Link>
      </p>
    </div>
  );
}
