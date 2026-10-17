namespace HoneyDrunk.Payments.Stripe;

/// <summary>
/// Stripe meter event payload captured before SDK transport.
/// </summary>
/// <param name="EventName">Stripe meter event name.</param>
/// <param name="CustomerKey">Stripe customer id or configured Stripe meter customer key.</param>
/// <param name="Units">Usage units.</param>
/// <param name="OccurredAtUtc">UTC timestamp when the metered usage occurred.</param>
/// <param name="IdempotencyKey">Per-event idempotency key used for Stripe deduplication.</param>
/// <param name="CorrelationId">Correlation identifier for traceability.</param>
/// <param name="Metadata">Bounded non-PII metadata.</param>
public sealed record StripeMeterEvent(
    string EventName,
    string CustomerKey,
    long Units,
    DateTimeOffset OccurredAtUtc,
    string IdempotencyKey,
    string CorrelationId,
    IReadOnlyDictionary<string, string> Metadata);
