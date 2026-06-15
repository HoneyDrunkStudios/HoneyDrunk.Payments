# Changelog

## 0.1.0 - 2026-06-14

- Add `HoneyDrunk.Payments.Abstractions` for provider-neutral checkout,
  subscription lifecycle, webhook normalization, and invoice reconciliation
  contracts.
- Stand up `HoneyDrunk.Payments.Stripe` with Stripe.NET-backed meter-event
  transport, durable-buffered Kernel `IBillingEventEmitter` composition,
  Checkout subscription creation with Stripe Tax enabled, subscription
  read/cancel, signed webhook normalization, invoice reconciliation snapshots,
  and explicit Stripe API version pinning.
- Add `HoneyDrunk.Payments.Tests.ProviderTesting`, a test-scoped reusable
  provider-neutral assertion harness that Stripe `.Tests.Unit` facts already
  run for checkout, lifecycle, webhook, invoice, and metered-billing behavior.
