using KernelBillingEvent = HoneyDrunk.Kernel.Abstractions.Tenancy.BillingEvent;
using KernelBillingEventEmitter = HoneyDrunk.Kernel.Abstractions.Tenancy.IBillingEventEmitter;

namespace HoneyDrunk.Payments.Stripe;

/// <summary>
/// Emits Grid billing events into Stripe metered billing.
/// </summary>
public sealed class StripeBillingEventEmitter(IStripeMeteredBillingClient client) : KernelBillingEventEmitter
{
    internal const string BillingEventIdAttributeKey = "billing_event_id";

    private readonly IStripeMeteredBillingClient meteredBillingClient = client ?? throw new ArgumentNullException(nameof(client));

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
        var billingEventId = GetBillingEventId(billingEvent);

        var meterEvent = new StripeMeterEvent(
            $"{billingEvent.EventType}.{billingEvent.OperationKey}",
            billingEvent.TenantId.ToString(),
            billingEvent.Units,
            billingEvent.OccurredAtUtc,
            billingEventId,
            billingEvent.CorrelationId,
            billingEvent.Attributes);

        await meteredBillingClient.RecordMeterEventAsync(meterEvent, cancellationToken).ConfigureAwait(false);
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
