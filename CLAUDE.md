# HoneyDrunk.Payments

HoneyDrunk.Payments is the shared payment-provider boundary for HoneyDrunk
products. It owns provider-neutral payment contracts and provider-specific
packages such as `HoneyDrunk.Payments.Stripe`.

Authoritative Architecture context lives in
`HoneyDrunk.Architecture/repos/HoneyDrunk.Payments/`. Keep this repository
aligned with that context before changing public contracts, provider package
boundaries, billing-event behavior, or Stripe composition.

Local validation:

```powershell
dotnet restore .\HoneyDrunk.Payments.slnx
dotnet build .\HoneyDrunk.Payments.slnx -c Release --no-restore
dotnet test .\HoneyDrunk.Payments.slnx -c Release --no-build
```

Provider packages must keep SDK-specific code, provider secrets, webhook
validation, and provider metadata policy inside the provider boundary. Product
nodes should depend on `HoneyDrunk.Payments.Abstractions` and choose providers
in composition.
