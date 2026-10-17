namespace HoneyDrunk.Payments.Stripe;

/// <summary>
/// Resolves Stripe API keys at call time from the host composition root.
/// </summary>
public interface IStripeApiKeyProvider
{
    /// <summary>
    /// Gets the Stripe API key for the current operation.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The Stripe API key.</returns>
    ValueTask<string> GetApiKeyAsync(CancellationToken cancellationToken);
}
