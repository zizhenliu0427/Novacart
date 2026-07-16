# PE-9 — AI Customer Support Chatbot

> **Status:** Complete (2026-07-16). Multi-provider AI chat with FAQ fallback.

## Providers

| Provider | Config `Chatbot:Provider` | Use case |
|----------|---------------------------|----------|
| **Disabled** | `Disabled` (default) | FAQ keyword fallback only |
| **OpenAI** | `OpenAI` | Production (`gpt-4o-mini` default) |
| **Ollama** | `Ollama` | Local / Docker self-hosted LLM |
| **Claude** | `Claude` | Anthropic API (`claude-3-5-haiku-20241022` default) |

All API keys stay **server-side** — the browser calls `POST /api/support/chat` only.

## Configuration

```json
"Chatbot": {
  "Enabled": true,
  "Provider": "OpenAI",
  "Model": "gpt-4o-mini",
  "MaxTokens": 512,
  "MaxHistoryMessages": 10,
  "RateLimitPerMinute": 10,
  "OpenAI": { "ApiKey": "sk-..." },
  "Ollama": { "BaseUrl": "http://ollama:11434", "Model": "llama3.2" },
  "Claude": { "ApiKey": "sk-ant-...", "Model": "claude-3-5-haiku-20241022" }
}
```

Environment variables (docker-compose / `.env`):

```env
Chatbot__Enabled=true
Chatbot__Provider=Claude
Chatbot__Claude__ApiKey=sk-ant-...
# or OpenAI__ApiKey via Chatbot__OpenAI__ApiKey
```

## API

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| `POST` | `/api/support/chat` | Optional | Send message + history |
| `GET` | `/api/support/faq?locale=en` | Public | Static FAQ list |

Gateway routes `/api/support/*` → **order-cluster** (needs order DB for signed-in context).

## Context injection

- Static FAQ (`Novacart.Core/Data/support-faq.{en,zh}.json`)
- **Signed-in users:** last 3 orders (number suffix, status, total, date — no full address)
- **Guests:** FAQ only; system prompt instructs not to invent order data

## Privacy

- Opt-in gate in frontend (`novacart_chat_opt_in` localStorage)
- PII redaction (email, phone) before LLM + logs
- Logs: provider, locale, hashed user id — **no full conversation persistence** in MVP

## Frontend

- `ChatWidget` — floating panel on all locale pages
- `useChatSupport` hook — `optionalAuth` API calls
- i18n namespace: `chatSupport`

## Rate limiting

- Gateway: 10 req/min/IP on `/api/support/*`
- Order API: named policy `chat` (same limit)

## Related

- [TODO.md § PE-9](../TODO.md)
- `backend/Novacart.Core/Infrastructure/Chatbot/`
