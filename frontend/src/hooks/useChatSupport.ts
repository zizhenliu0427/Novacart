'use client';

import { useCallback, useEffect, useRef, useState } from 'react';
import { useLocale } from 'next-intl';
import { apiCall } from '@/lib/api';
import type {
  ChatHistoryMessage,
  ChatMessage,
  SendChatMessageResponse,
  SupportFaqItem,
} from '@/types/chat';

const OPT_IN_KEY = 'novacart_chat_opt_in';

export function useChatSupport() {
  const locale = useLocale();
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [faq, setFaq] = useState<SupportFaqItem[]>([]);
  const [isSending, setIsSending] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [optIn, setOptIn] = useState(false);
  const sessionIdRef = useRef(typeof crypto !== 'undefined' ? crypto.randomUUID() : 'session');

  useEffect(() => {
    if (typeof window === 'undefined') return;
    setOptIn(window.localStorage.getItem(OPT_IN_KEY) === '1');
  }, []);

  useEffect(() => {
    let cancelled = false;
    apiCall<SupportFaqItem[]>(`/support/faq?locale=${locale}`, { optionalAuth: true })
      .then((items) => {
        if (!cancelled) setFaq(items);
      })
      .catch(() => {
        if (!cancelled) setFaq([]);
      });
    return () => {
      cancelled = true;
    };
  }, [locale]);

  const acceptOptIn = useCallback(() => {
    if (typeof window !== 'undefined') {
      window.localStorage.setItem(OPT_IN_KEY, '1');
    }
    setOptIn(true);
  }, []);

  const sendMessage = useCallback(
    async (text: string) => {
      const trimmed = text.trim();
      if (!trimmed || isSending) return;

      setIsSending(true);
      setError(null);

      const userMessage: ChatMessage = {
        role: 'user',
        content: trimmed,
        at: new Date().toISOString(),
      };
      setMessages((prev) => [...prev, userMessage]);

      const history: ChatHistoryMessage[] = [...messages, userMessage]
        .slice(-10)
        .map(({ role, content }) => ({ role, content }));

      try {
        const response = await apiCall<SendChatMessageResponse>('/support/chat', {
          method: 'POST',
          optionalAuth: true,
          body: {
            message: trimmed,
            history,
            locale,
          },
        });

        setMessages((prev) => [
          ...prev,
          {
            role: 'assistant',
            content: response.reply,
            at: new Date().toISOString(),
          },
        ]);
      } catch (err) {
        const message = err instanceof Error ? err.message : 'Failed';
        setError(message);
      } finally {
        setIsSending(false);
      }
    },
    [isSending, locale, messages],
  );

  return {
    messages,
    faq,
    sendMessage,
    isSending,
    error,
    optIn,
    acceptOptIn,
    sessionId: sessionIdRef.current,
  };
}
