namespace HoneyDrunk.Payments.Abstractions;

/// <summary>
/// Provider invoice reconciliation snapshot.
/// </summary>
/// <param name="Provider">Payment provider name.</param>
/// <param name="ProviderInvoiceId">Provider invoice identifier.</param>
/// <param name="ProviderCustomerId">Provider customer identifier, when assigned.</param>
/// <param name="ProviderSubscriptionId">Provider subscription identifier, when assigned.</param>
/// <param name="Status">Provider invoice status.</param>
/// <param name="Currency">Provider invoice currency code.</param>
/// <param name="AmountDue">Invoice amount due in the provider's minor currency unit.</param>
/// <param name="AmountPaid">Invoice amount paid in the provider's minor currency unit.</param>
/// <param name="AmountRemaining">Invoice amount remaining in the provider's minor currency unit.</param>
/// <param name="PeriodStart">Invoice billing period start time in UTC.</param>
/// <param name="PeriodEnd">Invoice billing period end time in UTC.</param>
/// <param name="PaidAt">Invoice payment completion time in UTC, when paid.</param>
/// <param name="HostedInvoiceUrl">Provider-hosted invoice URL, when available.</param>
/// <param name="InvoicePdfUrl">Provider invoice PDF URL, when available.</param>
/// <param name="TenantId">HoneyDrunk tenant identifier, when present in provider metadata.</param>
/// <param name="ProjectId">Product project identifier, when present in provider metadata.</param>
/// <param name="TierName">Product tier name, when present in provider metadata.</param>
/// <param name="Metadata">Additional provider metadata normalized as non-secret key/value pairs.</param>
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
