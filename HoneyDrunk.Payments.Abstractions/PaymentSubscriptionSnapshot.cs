namespace HoneyDrunk.Payments.Abstractions;

/// <summary>
/// Provider subscription snapshot used by payment workflows.
/// </summary>
/// <param name="Provider">Payment provider name.</param>
/// <param name="ProviderSubscriptionId">Provider subscription identifier.</param>
/// <param name="ProviderCustomerId">Provider customer identifier, when assigned.</param>
/// <param name="Status">Provider subscription status.</param>
/// <param name="TenantId">HoneyDrunk tenant identifier, when present in provider metadata.</param>
/// <param name="ProjectId">Product project identifier, when present in provider metadata.</param>
/// <param name="TierName">Product tier name, when present in provider metadata.</param>
/// <param name="CancelAtPeriodEnd">Whether the subscription is scheduled to cancel at the current period end.</param>
/// <param name="CanceledAt">Provider cancellation time in UTC, when canceled.</param>
/// <param name="CreatedAt">Provider subscription creation time in UTC.</param>
/// <param name="LatestInvoiceId">Latest provider invoice identifier, when available.</param>
/// <param name="Metadata">Additional provider metadata normalized as non-secret key/value pairs.</param>
public sealed record PaymentSubscriptionSnapshot(
    string Provider,
    string ProviderSubscriptionId,
    string? ProviderCustomerId,
    string? Status,
    string? TenantId,
    string? ProjectId,
    string? TierName,
    bool CancelAtPeriodEnd,
    DateTime? CanceledAt,
    DateTime CreatedAt,
    string? LatestInvoiceId,
    IReadOnlyDictionary<string, string> Metadata);
