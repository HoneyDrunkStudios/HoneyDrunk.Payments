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
        StripeMetadataPolicy.ValidateOutboundMetadata(billingEvent.Attributes, "billingEvent.Attributes");
        var billingEventId = GetBillingEventId(billingEvent);

        var meterEvent = new StripeMeterEvent(
            $"{billingEvent.EventType}.{billingEvent.OperationKey}",
            billingEvent.TenantId.ToString(),
            billingEvent.Units,
            billingEvent.OccurredAtUtc,
            billingEventId,
            billingEvent.CorrelationId,
            billingEvent.Attributes);

        await buffer.EnqueueAsync(meterEvent, cancellationToken).ConfigureAwait(false);
    }

    private static string GetBillingEventId(KernelBillingEvent billingEvent)
    {
        if (billingEvent.Attributes.TryGetValue(BillingEventIdAttributeKey, out var billingEventId)
            && !string.IsNullOrWhiteSpace(billingEventId))
        {
            return billingEventId;
        }

        throw new ArgumentException(
            $"Billing event attributes must include a non-empty '{BillingEventIdAttributeKey}' value for provider idempotency.",
            nameof(billingEvent));
    }

    private static void ValidateUnits(long units)
    {
        if (units <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(units), units, "Billing event units must be positive.");
        }
    }
}
