namespace HoneyDrunk.Payments.Stripe;

/// <summary>
/// Stripe Checkout session snapshot.
/// </summary>
/// <param name="SessionId">Stripe Checkout session identifier.</param>
/// <param name="Url">Hosted Checkout URL.</param>
/// <param name="TenantId">Payments tenant identifier.</param>
/// <param name="ProjectId">Payments project identifier.</param>
/// <param name="TierName">Payments tier name.</param>
/// <param name="StripeCustomerId">Stripe customer identifier, when assigned.</param>
/// <param name="StripeSubscriptionId">Stripe subscription identifier, when assigned.</param>
public sealed record StripeCheckoutSessionSnapshot(
    string SessionId,
    string? Url,
    string TenantId,
    string ProjectId,
    string TierName,
    string? StripeCustomerId,
    string? StripeSubscriptionId);
