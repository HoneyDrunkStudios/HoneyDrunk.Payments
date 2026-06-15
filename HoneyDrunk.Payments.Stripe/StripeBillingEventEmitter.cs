using HoneyDrunk.Kernel.Abstractions.Tenancy;

namespace HoneyDrunk.Payments.Stripe;

/// <summary>
/// Emits Grid billing events into Stripe metered billing.
/// </summary>
public sealed class StripeBillingEventEmitter(IStripeMeteredBillingClient? client = null) : IBillingEventEmitter
{
    private readonly IStripeMeteredBillingClient meteredBillingClient = client ?? new NoopStripeMeteredBillingClient();

    /// <inheritdoc />
    public async ValueTask EmitAsync(BillingEvent billingEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(billingEvent);

        if (billingEvent.TenantId.IsInternal)
        {
            return;
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(billingEvent.EventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(billingEvent.OperationKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(billingEvent.CorrelationId);

        ValidateUnits(billingEvent.Units);

        var meterEvent = new StripeMeterEvent(
            $"{billingEvent.EventType}.{billingEvent.OperationKey}",
            billingEvent.TenantId.ToString(),
            billingEvent.Units,
            billingEvent.OccurredAtUtc,
            billingEvent.CorrelationId,
            billingEvent.Attributes);

        await meteredBillingClient.RecordMeterEventAsync(meterEvent, cancellationToken).ConfigureAwait(false);
    }

    private static void ValidateUnits(long units)
    {
        if (units <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(units), units, "Billing event units must be positive.");
        }
    }
}
