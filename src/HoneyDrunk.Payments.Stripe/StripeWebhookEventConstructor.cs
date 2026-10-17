using Stripe;

namespace HoneyDrunk.Payments.Stripe;

/// <summary>
/// Stripe.NET signed webhook event constructor.
/// </summary>
internal sealed class StripeWebhookEventConstructor : IStripeWebhookEventConstructor
{
    /// <inheritdoc />
    public Event ConstructEvent(string payload, string signatureHeader, string webhookSecret) =>
        EventUtility.ConstructEvent(
            payload,
            signatureHeader,
            webhookSecret,
            tolerance: 300,
            throwOnApiVersionMismatch: false);
}
