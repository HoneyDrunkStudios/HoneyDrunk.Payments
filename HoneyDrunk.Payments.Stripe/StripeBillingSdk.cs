using Stripe;
using Stripe.Billing;
using StripeCheckout = Stripe.Checkout;

namespace HoneyDrunk.Payments.Stripe;

/// <summary>
/// Stripe.NET service wrapper.
/// </summary>
internal sealed class StripeBillingSdk : IStripeBillingSdk
{
    private readonly string apiKey;
    private readonly MeterEventService meterEvents = new();
    private readonly StripeCheckout.SessionService checkoutSessions = new();
    private readonly SubscriptionService subscriptions = new();
    private readonly InvoiceService invoices = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="StripeBillingSdk"/> class.
    /// </summary>
    /// <param name="apiKey">Stripe API key.</param>
    public StripeBillingSdk(string apiKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        this.apiKey = apiKey;
    }

    /// <inheritdoc />
    public Task<MeterEvent> CreateMeterEventAsync(
        MeterEventCreateOptions options,
        string? idempotencyKey,
        CancellationToken cancellationToken) =>
        meterEvents.CreateAsync(options, CreateRequestOptions(idempotencyKey), cancellationToken);

    /// <inheritdoc />
    public Task<StripeCheckout.Session> CreateCheckoutSessionAsync(
        StripeCheckout.SessionCreateOptions options,
        string? idempotencyKey,
        CancellationToken cancellationToken) =>
        checkoutSessions.CreateAsync(options, CreateRequestOptions(idempotencyKey), cancellationToken);

    /// <inheritdoc />
    public Task<Subscription> GetSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken) =>
        subscriptions.GetAsync(subscriptionId, options: null, CreateRequestOptions(null), cancellationToken);

    /// <inheritdoc />
    public Task<Subscription> CancelSubscriptionAsync(
        string subscriptionId,
        SubscriptionCancelOptions options,
        string? idempotencyKey,
        CancellationToken cancellationToken) =>
        subscriptions.CancelAsync(subscriptionId, options, CreateRequestOptions(idempotencyKey), cancellationToken);

    /// <inheritdoc />
    public Event ConstructEvent(string payload, string signatureHeader, string webhookSecret) =>
        EventUtility.ConstructEvent(
            payload,
            signatureHeader,
            webhookSecret,
            tolerance: 300,
            throwOnApiVersionMismatch: false);

    /// <inheritdoc />
    public Task<Invoice> GetInvoiceAsync(string invoiceId, CancellationToken cancellationToken) =>
        invoices.GetAsync(invoiceId, options: null, CreateRequestOptions(null), cancellationToken);

    private RequestOptions CreateRequestOptions(string? idempotencyKey) =>
        new()
        {
            ApiKey = apiKey,
            IdempotencyKey = string.IsNullOrWhiteSpace(idempotencyKey) ? null : idempotencyKey,
        };
}
