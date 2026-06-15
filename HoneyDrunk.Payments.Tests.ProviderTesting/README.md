# HoneyDrunk.Payments.Tests.ProviderTesting

Reusable provider-neutral contract assertions for Payments provider test
projects.

Provider test projects reference this helper project, subclass
`PaymentProviderContractTests`, expose concrete `[Fact]` methods from a
`.Tests.Unit` project, and return a provider-specific
`PaymentProviderContractFixture`. The fixture supplies provider-neutral clients
for subscription lifecycle, webhook validation, invoice reconciliation, and
Kernel billing-event emission.

The assertion harness verifies:

- hosted checkout returns a provider-neutral snapshot;
- subscription read and cancellation return provider-neutral snapshots;
- signed webhook validation returns normalized event metadata;
- invoice reconciliation returns normalized invoice state;
- metered billing emission requires per-event `billing_event_id` idempotency.

Production projects must not reference this project.
