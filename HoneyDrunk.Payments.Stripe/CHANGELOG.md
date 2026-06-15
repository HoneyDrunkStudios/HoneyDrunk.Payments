# Changelog

## 0.1.0

- Add Stripe.NET adapter for metered billing events, Checkout subscriptions,
  signed webhook normalization, subscription lifecycle, and invoice
  reconciliation.
- Preserve original billing-event timestamps when creating Stripe meter events.
- Enforce checkout idempotency keys before calling Stripe Checkout.
