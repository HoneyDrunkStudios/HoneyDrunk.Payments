namespace HoneyDrunk.Payments.Abstractions;

/// <summary>
/// Reads provider invoices for payment reconciliation.
/// </summary>
public interface IPaymentInvoiceReconciliationClient
{
    /// <summary>
    /// Reads an invoice and returns the fields required for reconciliation.
    /// </summary>
    /// <param name="providerInvoiceId">Provider invoice identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The invoice reconciliation snapshot.</returns>
    ValueTask<PaymentInvoiceReconciliationSnapshot> ReconcileInvoiceAsync(
        string providerInvoiceId,
        CancellationToken cancellationToken = default);
}
