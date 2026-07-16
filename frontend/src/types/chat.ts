export type ChatRole = 'user' | 'assistant';

export interface ChatMessage {
  role: ChatRole;
  content: string;
  at: string;
}

export interface ChatHistoryMessage {
  role: ChatRole;
  content: string;
}

export interface SendChatMessageRequest {
  message: string;
  history: ChatHistoryMessage[];
  locale: string;
}

export interface SendChatMessageResponse {
  reply: string;
  source: 'ai' | 'faq';
  provider: string;
}

export interface SupportFaqItem {
  question: string;
  answer: string;
}
