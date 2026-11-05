# Changelog

## [0.1.2] - 2026-11-05

### Changed

- Refresh NuGet dependencies and adopt upstream package releases in dependency order. Preserve target frameworks and public contracts.

| Dependency | Previous | Updated |
| --- | --- | --- |
| HoneyDrunk.Kernel.Abstractions | 0.8.1 | 0.8.2 |
| HoneyDrunk.Standards | 0.3.0 | 0.4.3 |
| HoneyDrunk.Standards.Tests | 0.3.0 | 0.4.3 |
| Stripe.net | 52.4.2 | 53.1.0 |

## [0.1.1] - 2026-09-26

### Changed

- Refresh dependency and shared build-tooling versions; preserve target frameworks and existing public contracts. See the [repository dependency changes](../../CHANGELOG.md).

## [0.1.0] - 2026-06-14

- Add provider-neutral subscription checkout and lifecycle contracts.
- Add provider-neutral webhook validation and invoice reconciliation contracts.
- Require checkout idempotency keys so provider adapters can keep retries safe.
