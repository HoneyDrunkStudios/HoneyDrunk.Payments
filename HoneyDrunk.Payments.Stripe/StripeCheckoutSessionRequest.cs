namespace HoneyDrunk.Payments.Stripe;

/// <summary>
/// Request for creating a Stripe Checkout subscription session.
/// </summary>
/// <param name="TenantId">Payments tenant identifier.</param>
/// <param name="ProjectId">Payments project identifier.</param>
/// <param name="TierName">Payments tier name.</param>
/// <param name="StripePriceId">Stripe recurring price identifier.</param>
/// <param name="SuccessUrl">Checkout success redirect URL.</param>
/// <param name="CancelUrl">Checkout cancel redirect URL.</param>
/// <param name="IdempotencyKey">Stripe idempotency key for retry-safe checkout creation.</param>
/// <param name="StripeCustomerId">Existing Stripe customer identifier, when known.</param>
/// <param name="CustomerEmail">Customer email for Checkout-created customers.</param>
/// <param name="Quantity">Subscription item quantity.</param>
/// <param name="Metadata">Additional non-PII metadata.</param>
public sealed record StripeCheckoutSessionRequest(
    string TenantId,
    string ProjectId,
    string TierName,
    string StripePriceId,
    string SuccessUrl,
    string CancelUrl,
    string IdempotencyKey,
    string? StripeCustomerId = null,
    string? CustomerEmail = null,
    long Quantity = 1,
    IReadOnlyDictionary<string, string>? Metadata = null);
