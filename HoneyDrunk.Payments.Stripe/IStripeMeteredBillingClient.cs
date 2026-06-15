namespace HoneyDrunk.Payments.Stripe;

/// <summary>
/// Records Stripe metered-billing usage.
/// </summary>
public interface IStripeMeteredBillingClient
{
    /// <summary>
    /// Records a meter event.
    /// </summary>
    /// <param name="meterEvent">Meter event to record.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous record operation.</returns>
    ValueTask RecordMeterEventAsync(StripeMeterEvent meterEvent, CancellationToken cancellationToken);
}
