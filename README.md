# HoneyDrunk.Payments

Payments owns the Grid payment-provider boundary. The initial package is
`HoneyDrunk.Payments.Stripe`, which contains the Stripe.NET adapter for metered
usage, subscription lifecycle, signed webhook normalization, and invoice
reconciliation.

Provider-neutral contracts live in `HoneyDrunk.Payments.Abstractions`. Product
Nodes should depend on those contracts and keep provider choice in composition
code. `HoneyDrunk.Payments.Stripe` is one implementation of those contracts.

Product Nodes such as NovOutbox keep product-specific pricing, tenant binding,
and subscription-state persistence. They compose Payments provider packages
instead of carrying Stripe SDK code in product repos.

Provider packages should keep SDKs and provider-specific secrets behind their
own composition boundary. For Stripe, hosts provide `IStripeApiKeyProvider`
and `IStripeWebhookSecretProvider` backed by Vault / `ISecretStore`, then pass
per-event billing idempotency through the `billing_event_id` billing attribute
and the persisted provider customer mapping through `provider_customer_id`.
Kernel `IBillingEventEmitter` composition also requires an
`IStripeMeterEventBuffer`; product hosts own the durable at-least-once store and
use `StripeMeterEventReplayDispatcher` to drain accepted events to Stripe.
Checkout sessions enable Stripe Tax, and Stripe requests are pinned to API
version `2026-05-27.dahlia`.

Reusable provider contract assertions live in the approved test helper project
`HoneyDrunk.Payments.Tests.Unit`.
Provider `.Tests.Unit` projects subclass its `PaymentProviderContractTests`
harness and own the concrete
`[Fact]` methods, so checkout, subscription lifecycle, webhook normalization,
invoice reconciliation, and metered-billing behavior are checked through the
same abstraction contracts for Stripe and future providers. Production projects
must not reference this test-scoped helper.

Metered usage should not derive provider customer identity from HoneyDrunk
tenant ids. Product hosts persist the provider customer id returned by checkout,
subscription reads, or invoice reconciliation, then pass that provider mapping
to the selected Payments provider at the composition boundary.

Payments pins `HoneyDrunk.Kernel.Abstractions` to `0.7.0`, the current
Architecture baseline used by Grid Review. The billing contracts consumed here
(`BillingEvent`, `IBillingEventEmitter`, and `TenantId`) are present in that
baseline.
Issue
[HoneyDrunk.Payments#3](https://github.com/HoneyDrunkStudios/HoneyDrunk.Payments/issues/3)
tracks expanding the suite when a second provider exposes provider-specific
edge cases.

## Build

```powershell
dotnet restore .\HoneyDrunk.Payments.slnx
dotnet build .\HoneyDrunk.Payments.slnx -c Release --no-restore
dotnet test .\HoneyDrunk.Payments.slnx -c Release --no-build
```
