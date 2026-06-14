namespace HoneyDrunk.Payments.Stripe;

/// <summary>
/// Validates and normalizes signed Stripe webhook events.
/// </summary>
public interface IStripeWebhookEventValidator
{
    /// <summary>
    /// Validates a Stripe webhook signature and returns a normalized event snapshot.
    /// </summary>
    /// <param name="payload">Raw UTF-8 request body.</param>
    /// <param name="signatureHeader">Stripe-Signature header value.</param>
    /// <param name="webhookSecret">Stripe webhook endpoint secret.</param>
    /// <returns>The normalized webhook event.</returns>
    StripeWebhookEventSnapshot ValidateWebhookEvent(
        string payload,
        string signatureHeader,
        string webhookSecret);
}
