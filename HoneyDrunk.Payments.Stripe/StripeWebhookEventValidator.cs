using HoneyDrunk.Payments.Abstractions;
using Stripe;

namespace HoneyDrunk.Payments.Stripe;

/// <summary>
/// Stripe.NET-backed validator for signed Stripe webhook events.
/// </summary>
public sealed class StripeWebhookEventValidator :
    IStripeWebhookEventValidator,
    IPaymentWebhookEventValidator
{
    private const string ProviderName = PaymentProviderNames.Stripe;

    private readonly IStripeWebhookSecretProvider webhookSecretProvider;
    private readonly IStripeWebhookEventConstructor webhookEventConstructor;

    /// <summary>
    /// Initializes a new instance of the <see cref="StripeWebhookEventValidator"/> class.
    /// </summary>
    /// <param name="webhookSecretProvider">Stripe webhook secret provider.</param>
    public StripeWebhookEventValidator(IStripeWebhookSecretProvider webhookSecretProvider)
        : this(webhookSecretProvider, new StripeWebhookEventConstructor())
    {
    }

    internal StripeWebhookEventValidator(
        IStripeWebhookSecretProvider webhookSecretProvider,
        IStripeWebhookEventConstructor webhookEventConstructor)
    {
        this.webhookSecretProvider = webhookSecretProvider ?? throw new ArgumentNullException(nameof(webhookSecretProvider));
        this.webhookEventConstructor = webhookEventConstructor ?? throw new ArgumentNullException(nameof(webhookEventConstructor));
    }

    /// <inheritdoc />
    async ValueTask<PaymentWebhookEventSnapshot> IPaymentWebhookEventValidator.ValidateWebhookEventAsync(
        string payload,
        string signatureHeader,
        CancellationToken cancellationToken) =>
        ToPaymentWebhookEventSnapshot(await ValidateWebhookEventAsync(payload, signatureHeader, cancellationToken).ConfigureAwait(false));

    /// <inheritdoc />
    public async ValueTask<StripeWebhookEventSnapshot> ValidateWebhookEventAsync(
        string payload,
        string signatureHeader,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);
        ArgumentException.ThrowIfNullOrWhiteSpace(signatureHeader);

        var webhookSecret = await webhookSecretProvider.GetWebhookSecretAsync(cancellationToken).ConfigureAwait(false);
        ArgumentException.ThrowIfNullOrWhiteSpace(webhookSecret);

        var stripeEvent = webhookEventConstructor.ConstructEvent(payload, signatureHeader, webhookSecret);
        var dataObject = stripeEvent.Data?.Object;
        var metadata = dataObject is IHasMetadata metadataObject
            ? StripeMetadataPolicy.CopyInboundMetadata(metadataObject.Metadata)
            : new Dictionary<string, string>(StringComparer.Ordinal);

        return new StripeWebhookEventSnapshot(
            stripeEvent.Id,
            stripeEvent.Type,
            stripeEvent.Created,
            stripeEvent.Livemode,
            (dataObject as IHasId)?.Id,
            dataObject?.Object,
            metadata);
    }

    private static PaymentWebhookEventSnapshot ToPaymentWebhookEventSnapshot(StripeWebhookEventSnapshot webhookEvent) =>
        new(
            ProviderName,
            webhookEvent.EventId,
            webhookEvent.EventType,
            webhookEvent.CreatedAt,
            webhookEvent.Livemode,
            webhookEvent.ObjectId,
            webhookEvent.ObjectType,
            webhookEvent.Metadata);
}
