# Changelog

## [0.1.0] - 2026-06-14

- Add `HoneyDrunk.Payments.Abstractions` for provider-neutral checkout,
  subscription lifecycle, webhook normalization, and invoice reconciliation
  contracts.
- Stand up `HoneyDrunk.Payments.Stripe` with Stripe.NET-backed meter-event
  transport, durable-buffered Kernel `IBillingEventEmitter` composition,
  Checkout subscription creation with Stripe Tax enabled, subscription
  read/cancel, signed webhook normalization, invoice reconciliation snapshots,
  explicit Stripe API version pinning, and outbound provider metadata safety
  enforcement plus inbound provider metadata sanitization before normalized
  snapshots cross package boundaries.
- Add `HoneyDrunk.Payments.Tests.Unit`, a test-scoped reusable
  provider-neutral assertion harness that Stripe `.Tests.Unit` facts already
  run for checkout, lifecycle, webhook, invoice, and metered-billing behavior.
