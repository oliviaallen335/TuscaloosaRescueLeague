# Secrets

Do **not** commit API keys. For local dev:

```bash
cd Api
dotnet user-secrets init
```

## DeepSeek (AI narrative + name suggestion)

```bash
dotnet user-secrets set "DeepSeek:ApiKey" "YOUR_KEY_HERE"
```

If `DeepSeek:ApiKey` is empty, matching still works; **AI narrative** is skipped.

## Stripe (adoption fee payments)

```bash
dotnet user-secrets set "Stripe:SecretKey" "sk_test_..."
dotnet user-secrets set "Stripe:WebhookSecret" "whsec_..."   # from Stripe CLI or Dashboard webhooks
dotnet user-secrets set "Stripe:PublishableKey" "pk_test_..."  # optional, for future client-side use
```

- **SecretKey** — Required for creating Checkout Sessions.
- **WebhookSecret** — Required to verify webhook signatures. Use `stripe listen --forward-to localhost:5014/api/payments/webhook` to get a local secret.
- Without Stripe config, "Pay adoption fee" returns an error.

[Stripe Dashboard](https://dashboard.stripe.com/) | [Stripe CLI](https://stripe.com/docs/stripe-cli)
