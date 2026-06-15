# HoneyDrunk.Payments.Stripe

Stripe payment and billing adapter package for HoneyDrunk metered usage,
subscription lifecycle, signed webhook normalization, and invoice reconciliation.

`StripeBillingEventEmitter` maps Kernel `BillingEvent` records into
`StripeMeterEvent` payloads and sends them through `IStripeMeteredBillingClient`.
`StripeBillingClient` uses Stripe.NET for meter-event transport, Checkout-backed
subscription creation, subscription read/cancel, signed webhook normalization,
and invoice reconciliation snapshots. It also implements the provider-neutral
`HoneyDrunk.Payments.Abstractions` contracts for product code that should not
depend directly on Stripe-specific types.

Hosts construct `StripeBillingClient` with `IStripeApiKeyProvider` and
`IStripeWebhookSecretProvider`. Providers should resolve Stripe API keys and
webhook endpoint secrets from the host Vault / `ISecretStore` boundary at call
time; the Payments package does not accept or retain raw provider-secret strings
in public client state.

`StripeBillingEventEmitter` requires an `IStripeMeteredBillingClient`. Missing
composition is fail-closed instead of falling back to no-op transport.

Meter events publish `customer_key`, `value`, `billing_event_id`, and
`correlation_id` payload fields plus bounded non-PII metadata. Stripe meters must
be configured with matching customer and value mappings. Kernel `BillingEvent`
records must include a non-empty `billing_event_id` attribute; Payments uses it
as both the Stripe meter identifier and API idempotency key. `correlation_id`
remains trace metadata only.
