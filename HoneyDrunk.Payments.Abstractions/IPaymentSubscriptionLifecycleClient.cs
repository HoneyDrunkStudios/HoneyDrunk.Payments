namespace HoneyDrunk.Payments.Abstractions;

/// <summary>
/// Starts, reads, and cancels provider-backed subscriptions.
/// </summary>
public interface IPaymentSubscriptionLifecycleClient
{
    /// <summary>
    /// Creates a hosted checkout session for a subscription.
    /// </summary>
    /// <param name="request">Checkout session request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created checkout session snapshot.</returns>
    ValueTask<PaymentCheckoutSessionSnapshot> CreateCheckoutSessionAsync(
        PaymentCheckoutSessionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads a provider subscription snapshot.
    /// </summary>
    /// <param name="providerSubscriptionId">Provider subscription identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The subscription snapshot.</returns>
    ValueTask<PaymentSubscriptionSnapshot> GetSubscriptionAsync(
        string providerSubscriptionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a provider subscription.
    /// </summary>
    /// <param name="request">Cancellation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The canceled subscription snapshot.</returns>
    ValueTask<PaymentSubscriptionSnapshot> CancelSubscriptionAsync(
        PaymentSubscriptionCancellationRequest request,
        CancellationToken cancellationToken = default);
}
