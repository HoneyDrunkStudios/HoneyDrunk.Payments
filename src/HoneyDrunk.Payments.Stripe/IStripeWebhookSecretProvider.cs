namespace HoneyDrunk.Payments.Stripe;

/// <summary>
/// Resolves Stripe webhook endpoint secrets at call time from the host composition root.
/// </summary>
public interface IStripeWebhookSecretProvider
{
    /// <summary>
    /// Gets the Stripe webhook endpoint secret for the current operation.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The Stripe webhook endpoint secret.</returns>
    ValueTask<string> GetWebhookSecretAsync(CancellationToken cancellationToken);
}
