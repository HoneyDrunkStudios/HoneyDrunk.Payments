# HoneyDrunk.Payments

HoneyDrunk.Payments is the shared payment-provider boundary for HoneyDrunk
products. It owns provider-neutral payment contracts and provider-specific
packages such as `HoneyDrunk.Payments.Stripe`.

Read [README.md](README.md) and the current Abstractions/provider implementation before changing public contracts, package boundaries, billing events or Stripe composition. Internal governance context may supplement this public contract when access is available; do not copy private context into this repository.

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

## Shared conventions and delivery

Read the [shared engineering conventions](https://github.com/HoneyDrunkStudios/HoneyDrunk.Standards/blob/main/HoneyDrunk.Standards/docs/CONVENTIONS.md) and this repository's owning documentation before editing. Apply the parts relevant to this stack; preserve existing public contracts, dependency direction and repository-specific behavior. Verify shared capabilities in current code before reusing them; a catalog entry or scaffold is not an implemented integration.

Work within the selected request. Preserve unrelated changes and use a separate worktree when needed. Review the final diff, use Conventional Commits and ready-for-review PRs with exactly one accurate `Authorship:` line and a `Request:` line; include the authorship in commit trailers. Run meaningful checks for the affected behavior and report the reviewed/tested revision, failures and unrun checks. For documentation-only changes, check links, paths and instruction consistency. Preserve required checks and inspect actual latest-head Sonar new-code findings where analysis applies; do not suppress findings or weaken gates to obtain a pass. Legacy Grid Review is retired; do not restore its workers, queues or bypass labels. A configured replacement reviewer is not evidence of a completed review or enforcing merge check.

Read the [engineering guide](docs/engineering-guide.md) for repository-specific contracts and patterns.
