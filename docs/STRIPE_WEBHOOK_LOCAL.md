# Testing Stripe Webhooks Locally

> A step-by-step guide for testing Stripe webhook integrations in your local development environment using either Stripe CLI (recommended) or ngrok.

---

## Prerequisites

Before you begin, make sure you have the following set up:

1. **Stripe Account** — Sign up at [stripe.com](https://stripe.com) if you haven't already. A free test-mode account is sufficient.
2. **Stripe CLI** — Install from [stripe.com/docs/stripe-cli](https://stripe.com/docs/stripe-cli):
   ```bash
   # macOS (Homebrew)
   brew install stripe/stripe-cli/stripe

   # Windows (Scoop)
   scoop install stripe

   # Linux (apt)
   sudo apt install stripe
   ```
3. **ngrok Account & CLI** (only for Option B) — Sign up at [ngrok.com](https://ngrok.com) and install:
   ```bash
   # macOS (Homebrew)
   brew install ngrok

   # Or download from https://ngrok.com/download
   ```
4. **Stripe Test API Keys** — Go to [Stripe Dashboard → Developers → API keys](https://dashboard.stripe.com/test/apikeys) and copy:
   - `Publishable key` (starts with `pk_test_...`)
   - `Secret key` (starts with `sk_test_...`)
5. **Environment configured** — Ensure your `.env` or `appsettings.Development.json` contains the Stripe keys:
   ```env
   STRIPE_SECRET_KEY=sk_test_...
   STRIPE_PUBLISHABLE_KEY=pk_test_...
   ```

---

## Option A — Using Stripe CLI (Recommended)

The Stripe CLI is the simplest way to forward webhook events to your local server. No tunnel or dashboard configuration required.

### Step 1: Login to Stripe

```bash
stripe login
```

This opens your browser for authentication. Once complete, the CLI is linked to your Stripe account.

### Step 2: Start the Webhook Listener

```bash
stripe listen --forward-to localhost:5000/api/webhooks/stripe
```

On startup, the CLI prints a **webhook signing secret** — it looks like:

```
> Ready! Your webhook signing secret is whsec_1234567890abcdef...
```

### Step 3: Set the Webhook Signing Secret

Copy the `whsec_...` value and set it in your configuration:

**Option 1 — `.env` file:**
```env
STRIPE_WEBHOOK_SECRET=whsec_1234567890abcdef...
```

**Option 2 — `appsettings.Development.json`:**
```json
{
  "Stripe": {
    "WebhookSecret": "whsec_1234567890abcdef..."
  }
}
```

> **Note:** The signing secret changes each time you restart `stripe listen`. Update your config accordingly.

### Step 4: Trigger Test Events

In a separate terminal, trigger events to verify your webhook handler:

```bash
# Trigger a completed checkout session
stripe trigger checkout.session.completed

# Trigger a failed payment
stripe trigger payment_intent.payment_failed

# Trigger a refund
stripe trigger charge.refunded
```

You should see event delivery logs in the `stripe listen` terminal and corresponding handler output in your application logs.

---

## Option B — Using ngrok

Use ngrok when you need a stable, publicly accessible URL (e.g., for sharing with teammates or testing over longer periods).

### Step 1: Start ngrok

```bash
ngrok http 5000
```

ngrok outputs a public HTTPS URL like:

```
Forwarding  https://a1b2c3d4.ngrok-free.app -> http://localhost:5000
```

Copy the `https://...ngrok-free.app` URL.

### Step 2: Register the Webhook in Stripe Dashboard

1. Go to [Stripe Dashboard → Developers → Webhooks](https://dashboard.stripe.com/test/webhooks)
2. Click **"Add endpoint"**
3. Set the **Endpoint URL** to:
   ```
   https://<your-ngrok-url>.ngrok-free.app/api/webhooks/stripe
   ```
4. Under **"Select events to listen to"**, add the following events:
   - `checkout.session.completed`
   - `payment_intent.payment_failed`
   - `charge.refunded`
5. Click **"Add endpoint"** to save

### Step 3: Copy the Signing Secret

After creating the endpoint:

1. Click on the newly created endpoint
2. Under **"Signing secret"**, click **"Reveal"**
3. Copy the `whsec_...` value
4. Set it in your `.env` or `appsettings.Development.json` as described in Option A, Step 3

> **Important:** The ngrok URL changes every time you restart ngrok (on the free plan). You'll need to update the webhook endpoint URL in the Stripe Dashboard each time.

---

## Testing the Flow End-to-End

### Step 1: Start the Application

```bash
# Start all services (backend, frontend, PostgreSQL, Redis)
docker compose up --build -d

# Verify all services are running
docker compose ps
```

### Step 2: Start the Stripe CLI Listener

```bash
stripe listen --forward-to localhost:5000/api/webhooks/stripe
```

Copy the `whsec_...` signing secret and update your config if it has changed.

### Step 3: Walk Through the Checkout Flow

1. Open the app at **http://localhost:3000**
2. **Register** a new account or **login** with existing credentials
3. **Browse products** and add items to your cart
4. Go to your **cart** and click **Checkout**
5. On the Stripe checkout page, use the following **test card**:

   | Field            | Value                       |
   |------------------|-----------------------------|
   | **Card number**  | `4242 4242 4242 4242`       |
   | **Expiry date**  | Any future date (e.g. `12/30`) |
   | **CVC**          | Any 3 digits (e.g. `123`)  |
   | **Name**         | Any name                    |
   | **ZIP/Postcode** | Any valid code              |

6. Complete the payment

### Step 4: Verify the Result

- Check the `stripe listen` terminal — you should see `checkout.session.completed` delivered successfully
- In the app, navigate to **Order History** — the order should appear with a **`paid`** status
- Check backend logs for webhook processing output:
  ```bash
  docker compose logs -f backend
  ```

### Additional Test Cards

| Scenario               | Card Number              |
|------------------------|--------------------------|
| Successful payment     | `4242 4242 4242 4242`    |
| Requires authentication| `4000 0025 0000 3155`    |
| Declined               | `4000 0000 0000 9995`    |
| Insufficient funds     | `4000 0000 0000 9995`    |

See the full list: [Stripe Testing Docs](https://stripe.com/docs/testing#cards)

---

## Troubleshooting

### Webhook Signature Mismatch (`400 Bad Request`)

**Symptom:** Stripe CLI shows events forwarded, but the backend returns `400`.

**Causes & Fixes:**
- **Wrong signing secret** — Ensure the `whsec_...` value in your config matches the one printed by `stripe listen`. It changes on every restart.
- **Request body modified** — The webhook signature is computed against the raw request body. Ensure no middleware modifies the body before signature verification. In ASP.NET Core, use `Request.Body` directly (not a parsed model).
- **Clock skew** — Stripe rejects signatures with timestamps more than 5 minutes off. Sync your system clock.

### Port Conflicts

**Symptom:** `stripe listen --forward-to localhost:5000/...` returns connection refused.

**Fixes:**
- Verify the backend is running on port `5000`: `docker compose ps` or `lsof -i :5000`
- If using a different port, update the `--forward-to` URL accordingly
- If running inside Docker, make sure port `5000` is mapped in `docker-compose.yml`

### Docker Networking Issues

**Symptom:** Stripe CLI can't reach the backend running in Docker.

**Explanation:** `localhost` inside a Docker container refers to the container itself, not the host machine.

**Fixes:**
- Run `stripe listen` **on the host machine** (not inside a container) and forward to `localhost:5000` — this works if port 5000 is published in Docker Compose
- Alternatively, use `host.docker.internal` instead of `localhost` when running the CLI inside Docker:
  ```bash
  stripe listen --forward-to host.docker.internal:5000/api/webhooks/stripe
  ```

### ngrok Tunnel Not Receiving Events

**Fixes:**
- Open the ngrok inspector at **http://localhost:4040** to see incoming requests
- Verify the webhook endpoint URL in the Stripe Dashboard matches your current ngrok URL
- Free ngrok plans generate a new URL on each restart — update the Stripe Dashboard accordingly

### Events Not Triggering Handler Logic

**Fixes:**
- Check that your webhook controller is handling the correct event types (`checkout.session.completed`, etc.)
- Review backend logs: `docker compose logs -f backend`
- Ensure the Stripe webhook secret is loaded correctly at startup (log it on startup in dev mode if needed)

---

## Quick Reference

```bash
# Login to Stripe CLI
stripe login

# Listen for webhooks (local dev)
stripe listen --forward-to localhost:5000/api/webhooks/stripe

# Trigger test events
stripe trigger checkout.session.completed
stripe trigger payment_intent.payment_failed
stripe trigger charge.refunded

# Start ngrok tunnel
ngrok http 5000

# View ngrok traffic inspector
open http://localhost:4040
```
