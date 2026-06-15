# HoneyDrunk.Payments.ProviderTesting

Reusable provider-neutral contract tests for Payments provider packages.

Provider test projects reference this helper project, subclass
`PaymentProviderContractTests`, and return a provider-specific
`PaymentProviderContractFixture`. The fixture supplies provider-neutral
clients for subscription lifecycle, webhook validation, invoice reconciliation,
and Kernel billing-event emission.

The suite verifies:

- hosted checkout returns a provider-neutral snapshot;
- subscription read and cancellation return provider-neutral snapshots;
- signed webhook validation returns normalized event metadata;
- invoice reconciliation returns normalized invoice state;
- metered billing emission requires per-event `billing_event_id` idempotency.

Production projects must not reference this project.
