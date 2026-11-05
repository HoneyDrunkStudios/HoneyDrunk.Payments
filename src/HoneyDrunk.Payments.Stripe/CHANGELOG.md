# Changelog

## [0.1.2] - 2026-11-05

### Changed

- Refresh NuGet dependencies and adopt upstream package releases in dependency order. Preserve target frameworks and public contracts.

| Dependency | Previous | Updated |
| --- | --- | --- |
| HoneyDrunk.Kernel.Abstractions | 0.8.1 | 0.8.2 |
| HoneyDrunk.Standards | 0.3.0 | 0.4.3 |
| HoneyDrunk.Standards.Tests | 0.3.0 | 0.4.3 |
| Stripe.net | 52.4.2 | 53.1.0 |

## [0.1.1] - 2026-09-26

### Changed

- Refresh dependency and shared build-tooling versions; preserve target frameworks and existing public contracts. See the [repository dependency changes](../../CHANGELOG.md).

## [0.1.0] - 2026-06-14

- Add Stripe.NET adapter for metered billing events, Checkout subscriptions,
  signed webhook normalization, subscription lifecycle, and invoice
  reconciliation.
- Preserve original billing-event timestamps when creating Stripe meter events.
- Enforce checkout idempotency keys before calling Stripe Checkout.
- Enable Stripe Tax on Checkout subscription sessions.
- Pin Stripe API requests to Stripe API version `2026-05-27.dahlia`.
- Enforce outbound Stripe metadata bounds and sensitive-key/sensitive-value
  rejection before Checkout and meter-event provider calls.
- Validate reserved Payments metadata values before sending Checkout and
  subscription metadata to Stripe.
- Normalize Kernel billing event names into Stripe-safe meter names before
  durable enqueue, and classify provider-invalid buffered meter names as
  permanent replay failures.
- Reject email-shaped outbound values and explicit provider secret prefixes
  before Stripe transport without blocking opaque identifiers solely because
  they contain fragments such as `card`, `secret`, or `address`.
- Allow GUID-shaped product identifiers while still rejecting random-looking
  opaque secret values in outbound metadata.
- Strip sensitive inbound Stripe metadata and allow only known-safe product
  mapping keys before returning subscription, webhook, and invoice snapshots.
- Require per-event meter idempotency through the `billing_event_id` billing
  attribute instead of trace correlation.
- Require explicit provider customer mapping through the `provider_customer_id`
  billing attribute instead of deriving Stripe meter `customer_key` values from
  HoneyDrunk tenant ids.
- Require Kernel billing events to enqueue through `IStripeMeterEventBuffer`,
  with `StripeMeterEventReplayDispatcher` owning replay to Stripe transport.
- Classify Stripe meter events with stale or future timestamps as permanent
  failures so durable buffers can dead-letter or reconcile instead of retrying
  unchanged events indefinitely.
- Resolve Stripe API keys through `IStripeApiKeyProvider` instead of storing raw
  API-key strings in Payments client state.
- Resolve Stripe webhook endpoint secrets through `IStripeWebhookSecretProvider`
  instead of accepting raw webhook secrets on validator contracts.
- Split Stripe API-key transport and webhook-secret validation composition
  between `StripeBillingClient` and `StripeWebhookEventValidator`.
- Document Stripe meter configuration requirements for `customer_key` customer
  mapping and product-boundary diagnostics for meter-emission failures.
