# Changelog

## 0.1.0 - 2026-06-14

- Add `HoneyDrunk.Payments.Abstractions` for provider-neutral checkout,
  subscription lifecycle, webhook normalization, and invoice reconciliation
  contracts.
- Stand up `HoneyDrunk.Payments.Stripe` with Stripe.NET-backed meter-event
  transport, Checkout subscription creation, subscription read/cancel, signed
  webhook normalization, invoice reconciliation snapshots, and Kernel
  `IBillingEventEmitter` support.
- Add `HoneyDrunk.Payments.ProviderTesting`, a reusable provider-neutral
  contract-test harness that Stripe already runs for checkout, lifecycle,
  webhook, invoice, and metered-billing behavior.
