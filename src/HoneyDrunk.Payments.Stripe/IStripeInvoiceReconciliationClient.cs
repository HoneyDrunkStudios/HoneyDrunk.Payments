namespace HoneyDrunk.Payments.Stripe;

/// <summary>
/// Reads Stripe invoices for Payments reconciliation.
/// </summary>
public interface IStripeInvoiceReconciliationClient
{
    /// <summary>
    /// Reads a Stripe invoice and returns the fields required for Payments reconciliation.
    /// </summary>
    /// <param name="invoiceId">Stripe invoice identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The invoice reconciliation snapshot.</returns>
    ValueTask<StripeInvoiceReconciliationSnapshot> ReconcileInvoiceAsync(
        string invoiceId,
        CancellationToken cancellationToken = default);
}
