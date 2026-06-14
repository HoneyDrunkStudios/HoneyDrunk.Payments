namespace HoneyDrunk.Payments.Stripe;

/// <summary>
/// Starts, reads, and cancels Stripe-backed Payments subscriptions.
/// </summary>
public interface IStripeSubscriptionLifecycleClient
{
    /// <summary>
    /// Creates a Stripe Checkout session for a Payments subscription.
    /// </summary>
    /// <param name="request">Checkout session request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created Checkout session snapshot.</returns>
    ValueTask<StripeCheckoutSessionSnapshot> CreateCheckoutSessionAsync(
        StripeCheckoutSessionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads a Stripe subscription snapshot.
    /// </summary>
    /// <param name="subscriptionId">Stripe subscription identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The subscription snapshot.</returns>
    ValueTask<StripeSubscriptionSnapshot> GetSubscriptionAsync(
        string subscriptionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a Stripe subscription.
    /// </summary>
    /// <param name="request">Cancellation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The canceled subscription snapshot.</returns>
    ValueTask<StripeSubscriptionSnapshot> CancelSubscriptionAsync(
        StripeSubscriptionCancellationRequest request,
        CancellationToken cancellationToken = default);
}
