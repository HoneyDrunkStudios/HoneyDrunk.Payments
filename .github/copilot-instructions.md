# HoneyDrunk.Payments Copilot Instructions

Follow the HoneyDrunk Architecture instructions and repository context for
`HoneyDrunk.Payments` before changing public payment contracts, provider
packages, workflow wiring, or billing behavior.

Key local rules:

- Keep product-facing contracts provider-neutral in
  `HoneyDrunk.Payments.Abstractions`.
- Keep Stripe.NET and Stripe-specific transport details inside
  `HoneyDrunk.Payments.Stripe`.
- Do not accept raw provider secrets in public client state; use provider
  interfaces backed by the host secret store.
- Enforce outbound provider metadata bounds and sensitive-key rejection before
  sending data to Stripe.
- Kernel billing events must go through a durable host-owned meter-event buffer
  before Stripe replay.
- Run `dotnet test .\HoneyDrunk.Payments.slnx -c Release` for code changes.
