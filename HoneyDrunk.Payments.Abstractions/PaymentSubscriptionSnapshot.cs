namespace HoneyDrunk.Payments.Abstractions;

/// <summary>
/// Provider subscription snapshot used by payment workflows.
/// </summary>
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
