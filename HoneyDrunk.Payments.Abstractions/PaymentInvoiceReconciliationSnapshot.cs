namespace HoneyDrunk.Payments.Abstractions;

/// <summary>
/// Provider invoice reconciliation snapshot.
/// </summary>
public sealed record PaymentInvoiceReconciliationSnapshot(
    string Provider,
    string ProviderInvoiceId,
    string? ProviderCustomerId,
    string? ProviderSubscriptionId,
    string? Status,
    string? Currency,
    long AmountDue,
    long AmountPaid,
    long AmountRemaining,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    DateTime? PaidAt,
    string? HostedInvoiceUrl,
    string? InvoicePdfUrl,
    string? TenantId,
    string? ProjectId,
    string? TierName,
    IReadOnlyDictionary<string, string> Metadata);
