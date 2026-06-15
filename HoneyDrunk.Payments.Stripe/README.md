# HoneyDrunk.Payments.Stripe

Stripe payment and billing adapter package for HoneyDrunk metered usage,
subscription lifecycle, signed webhook normalization, and invoice reconciliation.

`StripeBillingEventEmitter` maps Kernel `BillingEvent` records into
`StripeMeterEvent` payloads and enqueues them through `IStripeMeterEventBuffer`.
Product hosts own that durable Tier 0 buffer. Replay workers drain the buffer
with `StripeMeterEventReplayDispatcher`, which sends accepted events through
`IStripeMeteredBillingClient`. `StripeBillingClient` uses Stripe.NET for
meter-event transport, Checkout-backed subscription creation, subscription
read/cancel, signed webhook normalization, and invoice reconciliation snapshots.
It also implements the provider-neutral `HoneyDrunk.Payments.Abstractions`
contracts for product code that should not depend directly on Stripe-specific
types.

Hosts construct `StripeBillingClient` with `IStripeApiKeyProvider` and
`IStripeWebhookSecretProvider`. Providers should resolve Stripe API keys and
webhook endpoint secrets from the host Vault / `ISecretStore` boundary at call
time; the Payments package does not accept or retain raw provider-secret strings
in public client state.

`StripeBillingEventEmitter` requires an `IStripeMeterEventBuffer`. Missing
composition is fail-closed instead of falling back to direct provider transport
or no-op transport.

Meter events publish `customer_key`, `value`, `billing_event_id`, and
`correlation_id` payload fields plus bounded non-PII metadata. Stripe meters must
set `customer_mapping.event_payload_key` to `customer_key`, and the usage value
mapping must read `value`. Kernel `BillingEvent` records must include
a non-empty `billing_event_id` attribute; Payments uses it as both the Stripe
meter identifier and API idempotency key. `correlation_id` remains trace metadata
only. The durable buffer should dedupe on the same `billing_event_id` and replay
at least once until Stripe accepts the event.

Checkout session creation enables Stripe Tax with `automatic_tax.enabled=true`.
Stripe requests are pinned to API version `2026-05-27.dahlia`; changing that pin
is a deliberate Payments provider upgrade.

Outbound caller metadata is bounded and rejected when keys look sensitive.
Inbound Stripe metadata is sanitized before subscription, webhook, and invoice
snapshots cross the provider-neutral boundary. Only known-safe keys currently
used for product mapping (`payments_tenant_id`, `project_id`, `tier_name`, and
`invoice_source`) are returned, and values that look like tokens, secrets, email
addresses, phone numbers, signatures, or card identifiers are stripped instead
of returned to product consumers.

Hosts should log meter-emission failures at the product boundary with tenant,
project, event type, operation key, billing event id, and correlation id. Do not
log Stripe API keys, webhook secrets, raw signatures, or full webhook payloads.
