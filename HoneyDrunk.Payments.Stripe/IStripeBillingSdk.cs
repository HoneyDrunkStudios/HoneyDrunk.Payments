using Stripe;
using Stripe.Billing;
using StripeCheckout = Stripe.Checkout;

namespace HoneyDrunk.Payments.Stripe;

/// <summary>
/// Internal Stripe SDK seam used for focused adapter tests.
/// </summary>
internal interface IStripeBillingSdk
{
    /// <summary>
    /// Creates a Stripe meter event.
    /// </summary>
    /// <param name="options">Stripe meter event options.</param>
    /// <param name="idempotencyKey">Optional idempotency key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created meter event.</returns>
    Task<MeterEvent> CreateMeterEventAsync(
        MeterEventCreateOptions options,
        string? idempotencyKey,
        CancellationToken cancellationToken);

    /// <summary>
    /// Creates a Stripe Checkout session.
    /// </summary>
    /// <param name="options">Stripe Checkout options.</param>
    /// <param name="idempotencyKey">Optional idempotency key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created Checkout session.</returns>
    Task<StripeCheckout.Session> CreateCheckoutSessionAsync(
        StripeCheckout.SessionCreateOptions options,
        string? idempotencyKey,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a Stripe subscription.
    /// </summary>
    /// <param name="subscriptionId">Stripe subscription identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The subscription.</returns>
    Task<Subscription> GetSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken);

    /// <summary>
    /// Cancels a Stripe subscription.
    /// </summary>
    /// <param name="subscriptionId">Stripe subscription identifier.</param>
    /// <param name="options">Stripe cancellation options.</param>
    /// <param name="idempotencyKey">Optional idempotency key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The canceled subscription.</returns>
    Task<Subscription> CancelSubscriptionAsync(
        string subscriptionId,
        SubscriptionCancelOptions options,
        string? idempotencyKey,
        CancellationToken cancellationToken);

    /// <summary>
    /// Constructs and validates a signed Stripe webhook event.
    /// </summary>
    /// <param name="payload">Raw webhook payload.</param>
    /// <param name="signatureHeader">Stripe-Signature header.</param>
    /// <param name="webhookSecret">Webhook endpoint secret.</param>
    /// <returns>The Stripe event.</returns>
    Event ConstructEvent(string payload, string signatureHeader, string webhookSecret);

    /// <summary>
    /// Retrieves a Stripe invoice.
    /// </summary>
    /// <param name="invoiceId">Stripe invoice identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The invoice.</returns>
    Task<Invoice> GetInvoiceAsync(string invoiceId, CancellationToken cancellationToken);
}
