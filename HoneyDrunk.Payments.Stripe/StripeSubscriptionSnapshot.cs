namespace HoneyDrunk.Payments.Stripe;

/// <summary>
/// Stripe subscription snapshot used by Payments billing workflows.
/// </summary>
/// <param name="SubscriptionId">Stripe subscription identifier.</param>
/// <param name="StripeCustomerId">Stripe customer identifier.</param>
/// <param name="Status">Stripe subscription status.</param>
/// <param name="TenantId">Payments tenant identifier, when present in metadata.</param>
/// <param name="ProjectId">Payments project identifier, when present in metadata.</param>
/// <param name="TierName">Payments tier name, when present in metadata.</param>
/// <param name="CancelAtPeriodEnd">Whether cancellation is scheduled at period end.</param>
/// <param name="CanceledAt">Cancellation timestamp, when canceled.</param>
/// <param name="CreatedAt">Stripe creation timestamp.</param>
/// <param name="LatestInvoiceId">Latest Stripe invoice identifier, when present.</param>
/// <param name="Metadata">Stripe metadata.</param>
public sealed record StripeSubscriptionSnapshot(
    string SubscriptionId,
    string? StripeCustomerId,
    string? Status,
    string? TenantId,
    string? ProjectId,
    string? TierName,
    bool CancelAtPeriodEnd,
    DateTime? CanceledAt,
    DateTime CreatedAt,
    string? LatestInvoiceId,
    IReadOnlyDictionary<string, string> Metadata);
