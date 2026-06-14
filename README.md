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

## Build

```powershell
dotnet restore .\HoneyDrunk.Payments.slnx
dotnet build .\HoneyDrunk.Payments.slnx -c Release --no-restore
dotnet test .\HoneyDrunk.Payments.slnx -c Release --no-build
```
