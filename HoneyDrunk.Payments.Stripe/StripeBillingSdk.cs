using Stripe;
using Stripe.Billing;
using StripeCheckout = Stripe.Checkout;

namespace HoneyDrunk.Payments.Stripe;

/// <summary>
/// Stripe.NET service wrapper.
/// </summary>
internal sealed class StripeBillingSdk : IStripeBillingSdk
{
    internal const string StripeApiVersion = "2026-05-27.dahlia";

    private readonly IStripeApiKeyProvider apiKeyProvider;
    private readonly MeterEventService meterEvents = new();
    private readonly StripeCheckout.SessionService checkoutSessions = new();
    private readonly SubscriptionService subscriptions = new();
    private readonly InvoiceService invoices = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="StripeBillingSdk"/> class.
    /// </summary>
    /// <param name="apiKeyProvider">Stripe API key provider.</param>
    public StripeBillingSdk(IStripeApiKeyProvider apiKeyProvider)
    {
        this.apiKeyProvider = apiKeyProvider ?? throw new ArgumentNullException(nameof(apiKeyProvider));
        EnsurePinnedStripeApiVersion();
    }

    /// <inheritdoc />
    public async Task<MeterEvent> CreateMeterEventAsync(
        MeterEventCreateOptions options,
        string? idempotencyKey,
        CancellationToken cancellationToken) =>
        await meterEvents.CreateAsync(
            options,
            await CreateRequestOptionsAsync(idempotencyKey, cancellationToken).ConfigureAwait(false),
            cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<StripeCheckout.Session> CreateCheckoutSessionAsync(
        StripeCheckout.SessionCreateOptions options,
        string? idempotencyKey,
        CancellationToken cancellationToken) =>
        await checkoutSessions.CreateAsync(
            options,
            await CreateRequestOptionsAsync(idempotencyKey, cancellationToken).ConfigureAwait(false),
            cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<Subscription> GetSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken) =>
        await subscriptions.GetAsync(
            subscriptionId,
            options: null,
            await CreateRequestOptionsAsync(null, cancellationToken).ConfigureAwait(false),
            cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<Subscription> CancelSubscriptionAsync(
        string subscriptionId,
        SubscriptionCancelOptions options,
        string? idempotencyKey,
        CancellationToken cancellationToken) =>
        await subscriptions.CancelAsync(
            subscriptionId,
            options,
            await CreateRequestOptionsAsync(idempotencyKey, cancellationToken).ConfigureAwait(false),
            cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<Invoice> GetInvoiceAsync(string invoiceId, CancellationToken cancellationToken) =>
        await invoices.GetAsync(
            invoiceId,
            options: null,
            await CreateRequestOptionsAsync(null, cancellationToken).ConfigureAwait(false),
            cancellationToken).ConfigureAwait(false);

    internal async ValueTask<RequestOptions> CreateRequestOptionsAsync(string? idempotencyKey, CancellationToken cancellationToken)
    {
        EnsurePinnedStripeApiVersion();

        var apiKey = await apiKeyProvider.GetApiKeyAsync(cancellationToken).ConfigureAwait(false);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);

        return new RequestOptions
        {
            ApiKey = apiKey,
            IdempotencyKey = string.IsNullOrWhiteSpace(idempotencyKey) ? null : idempotencyKey,
        };
    }

    private static void EnsurePinnedStripeApiVersion()
    {
        if (!StringComparer.Ordinal.Equals(StripeConfiguration.ApiVersion, StripeApiVersion))
        {
            throw new InvalidOperationException(
                $"Stripe.NET API version drifted to '{StripeConfiguration.ApiVersion}'. Update '{nameof(StripeApiVersion)}' deliberately before using this provider.");
        }
    }
}
