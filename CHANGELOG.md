# Changelog

## [0.1.1] - 2026-09-26

### Changed

- Refresh stable NuGet dependencies; preserve target frameworks and HoneyDrunk public contracts.
- Enable the existing NuGet release workflow with the organization package-publishing secret. Publication remains subject to normal build, test, security, review, and merge gates; no billing service is deployed by this change.
- Align the explicit Stripe API guard with Stripe.NET 52.4.2 (`2026-08-26.dahlia`); retain the guard and validate the additive Dahlia migration. Hosted account/webhook settings are unchanged.

| Dependency | Previous | Updated |
| --- | --- | --- |
| Stripe.net | 52.0.0 | 52.4.2 |



### Verified HoneyDrunk dependencies

- HoneyDrunk.Kernel.Abstractions: 0.7.0 -> 0.8.1 (verified on NuGet.org).
- HoneyDrunk.Standards: 0.2.9 -> 0.3.0 (verified on NuGet.org).
- HoneyDrunk.Standards.Tests: 0.2.9 -> 0.3.0 (verified on NuGet.org).

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
