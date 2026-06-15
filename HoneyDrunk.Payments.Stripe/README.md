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
meter identifier and API idempotency key. Kernel `BillingEvent` records must also
include a non-empty `provider_customer_id` attribute containing the persisted
Stripe customer id, usually `cus_...`, or another configured Stripe meter
customer key. Payments copies that value into `customer_key`; it does not derive
Stripe customer identity from HoneyDrunk tenant ids. `billing_event_id` and
`provider_customer_id` are routing fields and are not copied into arbitrary
Stripe metadata. `correlation_id` remains trace metadata only. The durable buffer
should dedupe on the same `billing_event_id` and replay at least once until
Stripe accepts the event.

Replay preserves original usage timestamps. `StripeBillingClient` rejects meter
events older than 35 days or more than five minutes in the future with
`StripeMeterEventPermanentFailureException`. Buffers should treat that exception
as a dead-letter/reconciliation signal rather than retrying the same event
unchanged.

Checkout session creation enables Stripe Tax with `automatic_tax.enabled=true`.
Stripe requests are pinned to API version `2026-05-27.dahlia`; changing that pin
is a deliberate Payments provider upgrade.

Outbound caller metadata and reserved Payments metadata values
(`payments_tenant_id`, `project_id`, and `tier_name`) are bounded and rejected
when values look sensitive.
Outbound provider-bound identifiers, including Stripe customer ids, price ids,
idempotency keys, meter identifiers, correlation ids, subscription ids, invoice
ids, and cancellation comments, reject sensitive-looking values before Stripe
transport.
Inbound Stripe metadata is sanitized before subscription, webhook, and invoice
snapshots cross the provider-neutral boundary. Only known-safe keys currently
used for product mapping (`payments_tenant_id`, `project_id`, `tier_name`, and
`invoice_source`) are returned, and values that look like tokens, secrets, email
addresses, phone numbers, signatures, or card identifiers are stripped instead
of returned to product consumers.

Hosts should log meter-emission failures at the product boundary with tenant,
project, event type, operation key, billing event id, and correlation id. Do not
log Stripe API keys, webhook secrets, raw signatures, or full webhook payloads.
