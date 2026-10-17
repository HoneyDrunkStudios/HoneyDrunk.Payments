namespace HoneyDrunk.Payments.Stripe;

/// <summary>
/// Replays durably buffered meter events to Stripe.
/// </summary>
/// <param name="client">Stripe metered billing client used to send buffered meter events to Stripe.</param>
public sealed class StripeMeterEventReplayDispatcher(IStripeMeteredBillingClient client)
{
    private readonly IStripeMeteredBillingClient client = client ?? throw new ArgumentNullException(nameof(client));

    /// <summary>
    /// Dispatches one previously buffered meter event to Stripe.
    /// </summary>
    /// <param name="meterEvent">Buffered meter event.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when Stripe accepts the event.</returns>
    public ValueTask DispatchAsync(StripeMeterEvent meterEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(meterEvent);
        return client.RecordMeterEventAsync(meterEvent, cancellationToken);
    }
}
