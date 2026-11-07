# HoneyDrunk.Payments

## Repository layout

Open `HoneyDrunk.Payments.slnx` from the repository root. Production projects live in `src/`, test and canary projects in `tests/`, and examples in `samples/` where present. Shared build configuration stays at the root; project names, namespaces and package identities are unchanged. Existing documentation and release-history locations are retained.


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
own composition boundary. For Stripe, API transport composes
`StripeBillingClient` with `IStripeApiKeyProvider`, while webhook endpoints
compose `StripeWebhookEventValidator` with `IStripeWebhookSecretProvider`.
Both providers should be backed by Vault / `ISecretStore` and scoped to the
host operation that needs them. Hosts pass per-event billing idempotency through
the `billing_event_id` billing attribute and the persisted provider customer
mapping through `provider_customer_id`. Kernel `IBillingEventEmitter`
composition also requires an `IStripeMeterEventBuffer`; product hosts own the
durable at-least-once store and use `StripeMeterEventReplayDispatcher` to drain
accepted events to Stripe. Checkout sessions enable Stripe Tax by default.
For payment tests in a sandbox with no tax origin, compose
`StripeBillingClient(apiKeyProvider, disableAutomaticTaxInSandbox: true)`.
The adapter rejects that option before making any Checkout request with a
live or unrecognized key. Stripe requests are pinned to API version
`2026-10-28.endive`.

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

Payments uses `HoneyDrunk.Kernel.Abstractions` `0.8.2` for the billing contracts
`BillingEvent`, `IBillingEventEmitter`, and `TenantId`.
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
