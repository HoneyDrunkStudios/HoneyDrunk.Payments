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
per-event billing idempotency through the `billing_event_id` billing attribute.

Reusable provider contract assertions live in
`HoneyDrunk.Payments.ProviderTesting`. Provider `.Tests.Unit` projects subclass
its `PaymentProviderContractTests` harness and own the concrete `[Fact]`
methods, so checkout, subscription lifecycle, webhook normalization, invoice
reconciliation, and metered-billing behavior are checked through the same
abstraction contracts for Stripe and future providers.
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
