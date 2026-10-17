namespace HoneyDrunk.Payments.Stripe;

/// <summary>
/// Stripe invoice reconciliation snapshot.
/// </summary>
/// <param name="InvoiceId">Stripe invoice identifier.</param>
/// <param name="StripeCustomerId">Stripe customer identifier.</param>
/// <param name="StripeSubscriptionId">Stripe subscription identifier, when present.</param>
/// <param name="Status">Stripe invoice status.</param>
/// <param name="Currency">Invoice currency.</param>
/// <param name="AmountDue">Amount due in minor currency units.</param>
/// <param name="AmountPaid">Amount paid in minor currency units.</param>
/// <param name="AmountRemaining">Amount remaining in minor currency units.</param>
/// <param name="PeriodStart">Invoice period start.</param>
/// <param name="PeriodEnd">Invoice period end.</param>
/// <param name="PaidAt">Paid timestamp, when present.</param>
/// <param name="HostedInvoiceUrl">Hosted invoice URL.</param>
/// <param name="InvoicePdfUrl">Invoice PDF URL.</param>
/// <param name="TenantId">Payments tenant identifier, when present in metadata.</param>
/// <param name="ProjectId">Payments project identifier, when present in metadata.</param>
/// <param name="TierName">Payments tier name, when present in metadata.</param>
/// <param name="Metadata">Invoice metadata.</param>
public sealed record StripeInvoiceReconciliationSnapshot(
    string InvoiceId,
    string? StripeCustomerId,
    string? StripeSubscriptionId,
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
