using Stripe;

namespace HoneyDrunk.Payments.Stripe;

/// <summary>
/// Internal Stripe webhook construction seam used for focused adapter tests.
/// </summary>
internal interface IStripeWebhookEventConstructor
{
    /// <summary>
    /// Constructs and validates a signed Stripe webhook event.
    /// </summary>
    /// <param name="payload">Raw webhook payload.</param>
    /// <param name="signatureHeader">Stripe-Signature header.</param>
    /// <param name="webhookSecret">Webhook endpoint secret.</param>
    /// <returns>The Stripe event.</returns>
    Event ConstructEvent(string payload, string signatureHeader, string webhookSecret);
}
