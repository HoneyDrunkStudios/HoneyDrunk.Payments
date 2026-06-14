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

Meter events publish `customer_key`, `value`, and `correlation_id` payload fields
plus bounded non-PII metadata. Stripe meters must be configured with matching
customer and value mappings.
