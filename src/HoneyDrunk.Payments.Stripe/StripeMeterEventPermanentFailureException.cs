namespace HoneyDrunk.Payments.Stripe;

/// <summary>
/// Exception thrown when a buffered Stripe meter event cannot be retried unchanged.
/// </summary>
public sealed class StripeMeterEventPermanentFailureException : InvalidOperationException
{
    internal StripeMeterEventPermanentFailureException(
        StripeMeterEventPermanentFailureReason reason,
        StripeMeterEvent meterEvent,
        string message)
        : base(message)
    {
        Reason = reason;
        MeterEvent = meterEvent;
    }

    /// <summary>
    /// Gets the permanent failure reason.
    /// </summary>
    public StripeMeterEventPermanentFailureReason Reason { get; }

    /// <summary>
    /// Gets the meter event that should be dead-lettered or reconciled instead of retried unchanged.
    /// </summary>
    public StripeMeterEvent MeterEvent { get; }
}
