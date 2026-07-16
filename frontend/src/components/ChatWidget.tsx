'use client';

import { useState } from 'react';
import { useTranslations } from 'next-intl';
import { useAuth } from '@/contexts/AuthContext';
import { useChatSupport } from '@/hooks/useChatSupport';
import { Button } from '@/components/ui/Button';
import { Card } from '@/components/ui/Card';

export function ChatWidget() {
  const t = useTranslations('chatSupport');
  const { user } = useAuth();
  const [open, setOpen] = useState(false);
  const [draft, setDraft] = useState('');
  const { messages, faq, sendMessage, isSending, error, optIn, acceptOptIn } = useChatSupport();

  const handleSend = async () => {
    if (!draft.trim()) return;
    const text = draft;
    setDraft('');
    await sendMessage(text);
  };

  return (
    <>
      <Button
        type="button"
        variant="primary"
        size="sm"
        className="fixed bottom-6 right-6 z-[9998] h-12 w-12 rounded-full p-0 shadow-hover"
        aria-label={open ? t('closeChat') : t('openChat')}
        aria-expanded={open}
        onClick={() => setOpen((v) => !v)}
      >
        <span aria-hidden className="text-lg">{open ? '×' : '💬'}</span>
      </Button>

      {open && (
        <Card
          className="fixed bottom-20 right-4 z-[9998] flex w-[min(100vw-2rem,24rem)] max-h-[min(70vh,32rem)] flex-col overflow-hidden shadow-hover sm:right-6"
          role="dialog"
          aria-modal="true"
          aria-labelledby="chat-support-title"
        >
          <div className="flex items-center justify-between border-b border-border px-4 py-3">
            <h2 id="chat-support-title" className="text-sm font-semibold text-ink">
              {t('title')}
            </h2>
            <button
              type="button"
              className="text-xl leading-none text-ink-muted hover:text-ink"
              aria-label={t('closeChat')}
              onClick={() => setOpen(false)}
            >
              ×
            </button>
          </div>

          {!optIn ? (
            <div className="flex flex-1 flex-col gap-3 overflow-y-auto p-4 text-sm text-ink-muted">
              <p>{t('optInIntro')}</p>
              <ul className="list-disc space-y-1 pl-5">
                {faq.slice(0, 3).map((item) => (
                  <li key={item.question}>{item.question}</li>
                ))}
              </ul>
              <Button type="button" variant="primary" size="sm" onClick={acceptOptIn}>
                {t('optInAccept')}
              </Button>
            </div>
          ) : (
            <>
              <div className="flex-1 space-y-3 overflow-y-auto p-4" aria-live="polite">
                {messages.length === 0 && (
                  <p className="text-sm text-ink-muted">{t('emptyState')}</p>
                )}
                {!user && (
                  <p className="rounded-lg bg-bg-subtle px-3 py-2 text-xs text-ink-muted">{t('signInHint')}</p>
                )}
                {messages.map((msg, idx) => (
                  <div
                    key={`${msg.at}-${idx}`}
                    className={`max-w-[90%] rounded-lg px-3 py-2 text-sm ${
                      msg.role === 'user'
                        ? 'ml-auto bg-accent text-accent-contrast'
                        : 'mr-auto bg-bg-subtle text-ink'
                    }`}
                  >
                    {msg.content}
                  </div>
                ))}
                {isSending && (
                  <p className="text-xs text-ink-muted">{t('thinking')}</p>
                )}
                {error && (
                  <p className="text-xs text-red-600" role="alert">
                    {error.includes('429') || error.toLowerCase().includes('many')
                      ? t('rateLimited')
                      : t('errorSend')}
                  </p>
                )}
              </div>

              {faq.length > 0 && messages.length === 0 && (
                <div className="border-t border-border px-4 py-2">
                  <p className="mb-1 text-xs font-medium text-ink-muted">{t('fallbackTitle')}</p>
                  <div className="flex flex-wrap gap-1">
                    {faq.slice(0, 3).map((item) => (
                      <button
                        key={item.question}
                        type="button"
                        className="rounded-full border border-border px-2 py-0.5 text-xs text-ink-muted hover:bg-bg-subtle"
                        onClick={() => sendMessage(item.question)}
                      >
                        {item.question}
                      </button>
                    ))}
                  </div>
                </div>
              )}

              <form
                className="flex gap-2 border-t border-border p-3"
                onSubmit={(e) => {
                  e.preventDefault();
                  void handleSend();
                }}
              >
                <input
                  type="text"
                  value={draft}
                  onChange={(e) => setDraft(e.target.value)}
                  placeholder={t('placeholder')}
                  className="min-w-0 flex-1 rounded-lg border border-border bg-surface px-3 py-2 text-sm text-ink outline-none focus:border-accent"
                  maxLength={2000}
                  disabled={isSending}
                  aria-label={t('placeholder')}
                />
                <Button type="submit" variant="primary" size="sm" disabled={isSending || !draft.trim()}>
                  {isSending ? t('sending') : t('send')}
                </Button>
              </form>
            </>
          )}
        </Card>
      )}
    </>
  );
}
