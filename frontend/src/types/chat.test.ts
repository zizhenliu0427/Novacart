import { describe, expect, it } from 'vitest';
import type { ChatMessage } from '@/types/chat';

describe('chat types', () => {
  it('accepts user and assistant roles', () => {
    const msg: ChatMessage = {
      role: 'user',
      content: 'Hello',
      at: new Date().toISOString(),
    };
    expect(msg.role).toBe('user');
  });
});
