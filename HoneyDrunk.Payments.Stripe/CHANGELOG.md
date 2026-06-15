# Changelog

## [0.1.0] - 2026-06-14

- Add Stripe.NET adapter for metered billing events, Checkout subscriptions,
  signed webhook normalization, subscription lifecycle, and invoice
  reconciliation.
- Preserve original billing-event timestamps when creating Stripe meter events.
- Enforce checkout idempotency keys before calling Stripe Checkout.
- Enable Stripe Tax on Checkout subscription sessions.
- Pin Stripe API requests to Stripe API version `2026-05-27.dahlia`.
- Enforce outbound Stripe metadata bounds and sensitive-key rejection before
  Checkout and meter-event provider calls.
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
- Document Stripe meter configuration requirements for `customer_key` customer
  mapping and product-boundary diagnostics for meter-emission failures.
