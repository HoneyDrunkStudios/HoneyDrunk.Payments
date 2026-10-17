# HoneyDrunk.Payments.Abstractions

Provider-neutral contracts for HoneyDrunk payment and billing workflows.

This package owns the stable consumer-facing surface for subscription checkout,
subscription lifecycle reads/cancellation, signed webhook normalization, and
invoice reconciliation. Product nodes should depend on these contracts and let
composition choose a provider package such as `HoneyDrunk.Payments.Stripe`.

Provider-specific identifiers are intentionally named as provider fields in the
contract snapshots. Secret values are not part of this package; provider secrets
must be resolved by host composition through Vault / `ISecretStore`.
