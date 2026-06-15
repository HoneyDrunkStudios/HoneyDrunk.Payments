namespace HoneyDrunk.Payments.Stripe;

/// <summary>
/// Durable buffer for Stripe meter events before provider delivery.
/// </summary>
public interface IStripeMeterEventBuffer
{
    /// <summary>
    /// Enqueues a meter event for durable at-least-once replay to Stripe.
    /// </summary>
    /// <param name="meterEvent">Stripe meter event.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the event is durably accepted.</returns>
    ValueTask EnqueueAsync(StripeMeterEvent meterEvent, CancellationToken cancellationToken);
}
