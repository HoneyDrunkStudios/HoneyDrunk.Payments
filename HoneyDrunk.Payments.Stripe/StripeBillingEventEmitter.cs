using KernelBillingEvent = HoneyDrunk.Kernel.Abstractions.Tenancy.BillingEvent;
using KernelBillingEventEmitter = HoneyDrunk.Kernel.Abstractions.Tenancy.IBillingEventEmitter;

namespace HoneyDrunk.Payments.Stripe;

/// <summary>
/// Enqueues Grid billing events for durable Stripe metered billing replay.
/// </summary>
/// <param name="buffer">Durable Stripe meter-event buffer used to store billing events before replay.</param>
public sealed class StripeBillingEventEmitter(IStripeMeterEventBuffer buffer) : KernelBillingEventEmitter
{
    internal const string BillingEventIdAttributeKey = "billing_event_id";
    internal const string ProviderCustomerIdAttributeKey = "provider_customer_id";

    private readonly IStripeMeterEventBuffer buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));

    /// <inheritdoc />
    public async ValueTask EmitAsync(KernelBillingEvent billingEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(billingEvent);

        if (billingEvent.TenantId.IsInternal)
        {
            return;
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(billingEvent.EventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(billingEvent.OperationKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(billingEvent.CorrelationId);
        ArgumentNullException.ThrowIfNull(billingEvent.Attributes);

        ValidateUnits(billingEvent.Units);
        var billingEventId = GetRequiredAttribute(billingEvent, BillingEventIdAttributeKey);
        var providerCustomerId = GetRequiredAttribute(billingEvent, ProviderCustomerIdAttributeKey);
        StripeMetadataPolicy.ValidateProviderReferenceValue(billingEventId, "billingEvent.Attributes");
        StripeMetadataPolicy.ValidateProviderReferenceValue(providerCustomerId, "billingEvent.Attributes");
        StripeMetadataPolicy.ValidateProviderReferenceValue(billingEvent.CorrelationId, "billingEvent.CorrelationId");
        var metadata = CreateMeterMetadata(billingEvent.Attributes);

        var meterEvent = new StripeMeterEvent(
            StripeMeterEventNamePolicy.CreateFromKernelEvent(billingEvent.EventType, billingEvent.OperationKey),
            providerCustomerId,
            billingEvent.Units,
            billingEvent.OccurredAtUtc,
            billingEventId,
            billingEvent.CorrelationId,
            metadata);

        await buffer.EnqueueAsync(meterEvent, cancellationToken).ConfigureAwait(false);
    }

    private static string GetRequiredAttribute(KernelBillingEvent billingEvent, string key)
    {
        if (billingEvent.Attributes.TryGetValue(key, out var value)
            && !string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        throw new ArgumentException(
            $"Billing event attributes must include a non-empty '{key}' value for provider metered billing.",
            nameof(billingEvent));
    }

    private static Dictionary<string, string> CreateMeterMetadata(IReadOnlyDictionary<string, string> attributes)
    {
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var attribute in attributes)
        {
            if (StringComparer.Ordinal.Equals(attribute.Key, BillingEventIdAttributeKey)
                || StringComparer.Ordinal.Equals(attribute.Key, ProviderCustomerIdAttributeKey))
            {
                continue;
            }

            metadata[attribute.Key] = attribute.Value;
        }

        StripeMetadataPolicy.ValidateOutboundMetadata(metadata, "billingEvent.Attributes");
        return metadata;
    }

    private static void ValidateUnits(long units)
    {
        if (units <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(units), units, "Billing event units must be positive.");
        }
    }
}
