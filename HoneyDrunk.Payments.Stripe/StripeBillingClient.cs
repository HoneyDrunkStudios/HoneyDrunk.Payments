using HoneyDrunk.Payments.Abstractions;
using Stripe;
using Stripe.Billing;
using System.Globalization;
using StripeCheckout = Stripe.Checkout;

namespace HoneyDrunk.Payments.Stripe;

/// <summary>
/// Stripe.NET-backed billing client for Payments.
/// </summary>
public sealed class StripeBillingClient :
    IStripeMeteredBillingClient,
    IStripeSubscriptionLifecycleClient,
    IStripeWebhookEventValidator,
    IStripeInvoiceReconciliationClient,
    IPaymentSubscriptionLifecycleClient,
    IPaymentWebhookEventValidator,
    IPaymentInvoiceReconciliationClient
{
    internal const string TenantMetadataKey = "payments_tenant_id";
    internal const string ProjectMetadataKey = "project_id";
    internal const string TierMetadataKey = "tier_name";
    internal const string MeterCustomerPayloadKey = "customer_key";
    internal const string MeterValuePayloadKey = "value";
    internal const string MeterEventIdPayloadKey = "billing_event_id";
    internal const string MeterCorrelationPayloadKey = "correlation_id";

    internal static readonly TimeSpan MaxMeterEventAge = TimeSpan.FromDays(35);
    internal static readonly TimeSpan MaxMeterEventFutureSkew = TimeSpan.FromMinutes(5);

    private const string ProviderName = PaymentProviderNames.Stripe;

    private readonly IStripeBillingSdk sdk;
    private readonly IStripeWebhookSecretProvider? webhookSecretProvider;
    private readonly TimeProvider timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="StripeBillingClient"/> class.
    /// </summary>
    /// <param name="apiKeyProvider">Stripe API key provider.</param>
    /// <param name="webhookSecretProvider">Stripe webhook secret provider.</param>
    public StripeBillingClient(
        IStripeApiKeyProvider apiKeyProvider,
        IStripeWebhookSecretProvider webhookSecretProvider)
        : this(new StripeBillingSdk(apiKeyProvider), webhookSecretProvider)
    {
    }

    internal StripeBillingClient(
        IStripeBillingSdk sdk,
        IStripeWebhookSecretProvider? webhookSecretProvider = null,
        TimeProvider? timeProvider = null)
    {
        this.sdk = sdk ?? throw new ArgumentNullException(nameof(sdk));
        this.webhookSecretProvider = webhookSecretProvider;
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    async ValueTask<PaymentCheckoutSessionSnapshot> IPaymentSubscriptionLifecycleClient.CreateCheckoutSessionAsync(
        PaymentCheckoutSessionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var session = await CreateCheckoutSessionAsync(
                new StripeCheckoutSessionRequest(
                    request.TenantId,
                    request.ProjectId,
                    request.TierName,
                    request.ProviderPriceId,
                    request.SuccessUrl,
                    request.CancelUrl,
                    request.IdempotencyKey,
                    request.ProviderCustomerId,
                    request.CustomerEmail,
                    request.Quantity,
                    request.Metadata),
                cancellationToken)
            .ConfigureAwait(false);

        return ToPaymentCheckoutSessionSnapshot(session);
    }

    /// <inheritdoc />
    async ValueTask<PaymentSubscriptionSnapshot> IPaymentSubscriptionLifecycleClient.GetSubscriptionAsync(
        string providerSubscriptionId,
        CancellationToken cancellationToken)
    {
        var subscription = await GetSubscriptionAsync(providerSubscriptionId, cancellationToken).ConfigureAwait(false);
        return ToPaymentSubscriptionSnapshot(subscription);
    }

    /// <inheritdoc />
    async ValueTask<PaymentSubscriptionSnapshot> IPaymentSubscriptionLifecycleClient.CancelSubscriptionAsync(
        PaymentSubscriptionCancellationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var subscription = await CancelSubscriptionAsync(
                new StripeSubscriptionCancellationRequest(
                    request.ProviderSubscriptionId,
                    request.InvoiceNow,
                    request.Prorate,
                    request.Reason,
                    request.IdempotencyKey),
                cancellationToken)
            .ConfigureAwait(false);

        return ToPaymentSubscriptionSnapshot(subscription);
    }

    /// <inheritdoc />
    async ValueTask<PaymentWebhookEventSnapshot> IPaymentWebhookEventValidator.ValidateWebhookEventAsync(
        string payload,
        string signatureHeader,
        CancellationToken cancellationToken) =>
        ToPaymentWebhookEventSnapshot(await ValidateWebhookEventAsync(payload, signatureHeader, cancellationToken).ConfigureAwait(false));

    /// <inheritdoc />
    async ValueTask<PaymentInvoiceReconciliationSnapshot> IPaymentInvoiceReconciliationClient.ReconcileInvoiceAsync(
        string providerInvoiceId,
        CancellationToken cancellationToken)
    {
        var invoice = await ReconcileInvoiceAsync(providerInvoiceId, cancellationToken).ConfigureAwait(false);
        return ToPaymentInvoiceReconciliationSnapshot(invoice);
    }

    /// <inheritdoc />
    public async ValueTask RecordMeterEventAsync(StripeMeterEvent meterEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(meterEvent);
        ArgumentException.ThrowIfNullOrWhiteSpace(meterEvent.EventName);
        ArgumentException.ThrowIfNullOrWhiteSpace(meterEvent.CustomerKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(meterEvent.IdempotencyKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(meterEvent.CorrelationId);
        StripeMetadataPolicy.ValidateProviderReferenceValue(meterEvent.CustomerKey, "meterEvent.CustomerKey");

        if (meterEvent.Units <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(meterEvent), meterEvent.Units, "Meter event units must be positive.");
        }

        ValidateMeterEventTimestamp(meterEvent, timeProvider.GetUtcNow());

        var payload = StripeMetadataPolicy.CopyOutboundMetadata(meterEvent.Metadata, "meterEvent.Metadata");
        payload[MeterCustomerPayloadKey] = meterEvent.CustomerKey;
        payload[MeterValuePayloadKey] = meterEvent.Units.ToString(CultureInfo.InvariantCulture);
        payload[MeterEventIdPayloadKey] = meterEvent.IdempotencyKey;
        payload[MeterCorrelationPayloadKey] = meterEvent.CorrelationId;

        var options = new MeterEventCreateOptions
        {
            EventName = meterEvent.EventName,
            Identifier = meterEvent.IdempotencyKey,
            Payload = payload,
            Timestamp = meterEvent.OccurredAtUtc.UtcDateTime,
        };

        await sdk
            .CreateMeterEventAsync(options, meterEvent.IdempotencyKey, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<StripeCheckoutSessionSnapshot> CreateCheckoutSessionAsync(
        StripeCheckoutSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateCheckoutRequest(request);

        var stripeCustomerId = string.IsNullOrWhiteSpace(request.StripeCustomerId)
            ? null
            : request.StripeCustomerId;

        var metadata = BuildPaymentsMetadata(
            request.TenantId,
            request.ProjectId,
            request.TierName,
            request.Metadata);

        var options = new StripeCheckout.SessionCreateOptions
        {
            Mode = "subscription",
            SuccessUrl = request.SuccessUrl,
            CancelUrl = request.CancelUrl,
            AutomaticTax = new StripeCheckout.SessionAutomaticTaxOptions
            {
                Enabled = true,
            },
            Customer = stripeCustomerId,
            CustomerEmail = stripeCustomerId is null ? request.CustomerEmail : null,
            ClientReferenceId = $"{request.TenantId}:{request.ProjectId}",
            Metadata = metadata,
            LineItems =
            [
                new StripeCheckout.SessionLineItemOptions
                {
                    Price = request.StripePriceId,
                    Quantity = request.Quantity,
                },
            ],
            SubscriptionData = new StripeCheckout.SessionSubscriptionDataOptions
            {
                Metadata = metadata,
            },
        };

        var session = await sdk
            .CreateCheckoutSessionAsync(options, request.IdempotencyKey, cancellationToken)
            .ConfigureAwait(false);

        return new StripeCheckoutSessionSnapshot(
            session.Id,
            session.Url,
            request.TenantId,
            request.ProjectId,
            request.TierName,
            session.CustomerId,
            session.SubscriptionId);
    }

    /// <inheritdoc />
    public async ValueTask<StripeSubscriptionSnapshot> GetSubscriptionAsync(
        string subscriptionId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subscriptionId);

        var subscription = await sdk
            .GetSubscriptionAsync(subscriptionId, cancellationToken)
            .ConfigureAwait(false);

        return ToSubscriptionSnapshot(subscription);
    }

    /// <inheritdoc />
    public async ValueTask<StripeSubscriptionSnapshot> CancelSubscriptionAsync(
        StripeSubscriptionCancellationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.SubscriptionId);

        var options = new SubscriptionCancelOptions
        {
            InvoiceNow = request.InvoiceNow,
            Prorate = request.Prorate,
            CancellationDetails = string.IsNullOrWhiteSpace(request.Reason)
                ? null
                : new SubscriptionCancellationDetailsOptions
                {
                    Comment = request.Reason,
                },
        };

        var subscription = await sdk
            .CancelSubscriptionAsync(request.SubscriptionId, options, request.IdempotencyKey, cancellationToken)
            .ConfigureAwait(false);

        return ToSubscriptionSnapshot(subscription);
    }

    /// <inheritdoc />
    public async ValueTask<StripeWebhookEventSnapshot> ValidateWebhookEventAsync(
        string payload,
        string signatureHeader,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);
        ArgumentException.ThrowIfNullOrWhiteSpace(signatureHeader);
        var provider = webhookSecretProvider
            ?? throw new InvalidOperationException("Stripe webhook validation requires an IStripeWebhookSecretProvider.");

        var webhookSecret = await provider.GetWebhookSecretAsync(cancellationToken).ConfigureAwait(false);
        ArgumentException.ThrowIfNullOrWhiteSpace(webhookSecret);

        var stripeEvent = sdk.ConstructEvent(payload, signatureHeader, webhookSecret);
        var dataObject = stripeEvent.Data?.Object;
        var metadata = dataObject is IHasMetadata metadataObject
            ? CopyInboundMetadata(metadataObject.Metadata)
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

    /// <inheritdoc />
    public async ValueTask<StripeInvoiceReconciliationSnapshot> ReconcileInvoiceAsync(
        string invoiceId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(invoiceId);

        var invoice = await sdk
            .GetInvoiceAsync(invoiceId, cancellationToken)
            .ConfigureAwait(false);

        var metadata = CopyInboundMetadata(invoice.Metadata);
        var subscriptionMetadata = CopyInboundMetadata(invoice.Parent?.SubscriptionDetails?.Metadata);
        var lookupMetadata = MergeMetadata(subscriptionMetadata, metadata);

        return new StripeInvoiceReconciliationSnapshot(
            invoice.Id,
            invoice.CustomerId,
            invoice.Parent?.SubscriptionDetails?.SubscriptionId,
            invoice.Status,
            invoice.Currency,
            invoice.AmountDue,
            invoice.AmountPaid,
            invoice.AmountRemaining,
            invoice.PeriodStart,
            invoice.PeriodEnd,
            invoice.StatusTransitions?.PaidAt,
            invoice.HostedInvoiceUrl,
            invoice.InvoicePdf,
            TryGetMetadata(lookupMetadata, TenantMetadataKey),
            TryGetMetadata(lookupMetadata, ProjectMetadataKey),
            TryGetMetadata(lookupMetadata, TierMetadataKey),
            metadata);
    }

    private static void ValidateMeterEventTimestamp(StripeMeterEvent meterEvent, DateTimeOffset now)
    {
        var oldestAccepted = now.Subtract(MaxMeterEventAge);
        if (meterEvent.OccurredAtUtc < oldestAccepted)
        {
            throw new StripeMeterEventPermanentFailureException(
                StripeMeterEventPermanentFailureReason.TimestampTooOld,
                meterEvent,
                "Stripe meter event timestamp is older than the accepted replay window and should be dead-lettered or reconciled instead of retried unchanged.");
        }

        var newestAccepted = now.Add(MaxMeterEventFutureSkew);
        if (meterEvent.OccurredAtUtc > newestAccepted)
        {
            throw new StripeMeterEventPermanentFailureException(
                StripeMeterEventPermanentFailureReason.TimestampTooNew,
                meterEvent,
                "Stripe meter event timestamp is too far in the future and should be dead-lettered or retried after clock reconciliation instead of sent unchanged.");
        }
    }

    private static void ValidateCheckoutRequest(StripeCheckoutSessionRequest request)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ProjectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.TierName);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.StripePriceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.SuccessUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.CancelUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.IdempotencyKey);

        if (string.IsNullOrWhiteSpace(request.StripeCustomerId)
            && string.IsNullOrWhiteSpace(request.CustomerEmail))
        {
            throw new ArgumentException(
                "Checkout requires either an existing Stripe customer id or a customer email.",
                nameof(request));
        }

        if (request.Quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request), request.Quantity, "Checkout quantity must be positive.");
        }
    }

    private static StripeSubscriptionSnapshot ToSubscriptionSnapshot(Subscription subscription)
    {
        ArgumentNullException.ThrowIfNull(subscription);

        var metadata = CopyInboundMetadata(subscription.Metadata);
        return new StripeSubscriptionSnapshot(
            subscription.Id,
            subscription.CustomerId,
            subscription.Status,
            TryGetMetadata(metadata, TenantMetadataKey),
            TryGetMetadata(metadata, ProjectMetadataKey),
            TryGetMetadata(metadata, TierMetadataKey),
            subscription.CancelAtPeriodEnd,
            subscription.CanceledAt,
            subscription.Created,
            subscription.LatestInvoiceId,
            metadata);
    }

    private static PaymentCheckoutSessionSnapshot ToPaymentCheckoutSessionSnapshot(StripeCheckoutSessionSnapshot session) =>
        new(
            ProviderName,
            session.SessionId,
            session.Url,
            session.TenantId,
            session.ProjectId,
            session.TierName,
            session.StripeCustomerId,
            session.StripeSubscriptionId);

    private static PaymentSubscriptionSnapshot ToPaymentSubscriptionSnapshot(StripeSubscriptionSnapshot subscription) =>
        new(
            ProviderName,
            subscription.SubscriptionId,
            subscription.StripeCustomerId,
            subscription.Status,
            subscription.TenantId,
            subscription.ProjectId,
            subscription.TierName,
            subscription.CancelAtPeriodEnd,
            subscription.CanceledAt,
            subscription.CreatedAt,
            subscription.LatestInvoiceId,
            subscription.Metadata);

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

    private static PaymentInvoiceReconciliationSnapshot ToPaymentInvoiceReconciliationSnapshot(
        StripeInvoiceReconciliationSnapshot invoice) =>
        new(
            ProviderName,
            invoice.InvoiceId,
            invoice.StripeCustomerId,
            invoice.StripeSubscriptionId,
            invoice.Status,
            invoice.Currency,
            invoice.AmountDue,
            invoice.AmountPaid,
            invoice.AmountRemaining,
            invoice.PeriodStart,
            invoice.PeriodEnd,
            invoice.PaidAt,
            invoice.HostedInvoiceUrl,
            invoice.InvoicePdfUrl,
            invoice.TenantId,
            invoice.ProjectId,
            invoice.TierName,
            invoice.Metadata);

    private static Dictionary<string, string> BuildPaymentsMetadata(
        string tenantId,
        string projectId,
        string tierName,
        IReadOnlyDictionary<string, string>? metadata)
    {
        var copy = StripeMetadataPolicy.CopyOutboundMetadata(metadata, "request.Metadata");
        copy[TenantMetadataKey] = tenantId;
        copy[ProjectMetadataKey] = projectId;
        copy[TierMetadataKey] = tierName;
        return copy;
    }

    private static Dictionary<string, string> CopyInboundMetadata(IReadOnlyDictionary<string, string>? metadata) =>
        StripeMetadataPolicy.CopyInboundMetadata(metadata);

    private static Dictionary<string, string> MergeMetadata(
        IReadOnlyDictionary<string, string> primary,
        IReadOnlyDictionary<string, string> overrides)
    {
        var merged = CopyInboundMetadata(primary);
        foreach (var item in overrides)
        {
            merged[item.Key] = item.Value;
        }

        return merged;
    }

    private static string? TryGetMetadata(IReadOnlyDictionary<string, string> metadata, string key) =>
        metadata.TryGetValue(key, out var value) ? value : null;
}
