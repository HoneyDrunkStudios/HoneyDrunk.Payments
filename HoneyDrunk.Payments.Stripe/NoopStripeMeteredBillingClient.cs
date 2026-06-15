namespace HoneyDrunk.Payments.Stripe;

internal sealed class NoopStripeMeteredBillingClient : IStripeMeteredBillingClient
{
    public ValueTask RecordMeterEventAsync(StripeMeterEvent meterEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(meterEvent);
        cancellationToken.ThrowIfCancellationRequested();

        return ValueTask.CompletedTask;
    }
}
