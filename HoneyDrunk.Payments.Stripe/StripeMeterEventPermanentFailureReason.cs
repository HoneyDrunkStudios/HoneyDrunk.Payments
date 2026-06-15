namespace HoneyDrunk.Payments.Stripe;

/// <summary>
/// Permanent provider-side reasons that make a Stripe meter event unsafe to retry unchanged.
/// </summary>
public enum StripeMeterEventPermanentFailureReason
{
    /// <summary>
    /// The meter event occurred before Stripe's accepted replay window.
    /// </summary>
    TimestampTooOld,

    /// <summary>
    /// The meter event timestamp is too far ahead of the provider clock.
    /// </summary>
    TimestampTooNew,

    /// <summary>
    /// The meter event name cannot be accepted by the Stripe meter-event API.
    /// </summary>
    InvalidEventName,
}
