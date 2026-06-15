namespace HoneyDrunk.Payments.Stripe;

/// <summary>
/// Provider-neutral Stripe meter event payload captured before SDK transport is wired.
/// </summary>
/// <param name="EventName">Stripe meter event name.</param>
/// <param name="CustomerKey">Tenant/customer key used by the Stripe bridge.</param>
/// <param name="Units">Usage units.</param>
/// <param name="OccurredAtUtc">UTC timestamp when the metered usage occurred.</param>
/// <param name="CorrelationId">Correlation identifier for idempotent traceability.</param>
/// <param name="Metadata">Bounded non-PII metadata.</param>
public sealed record StripeMeterEvent(
    string EventName,
    string CustomerKey,
    long Units,
    DateTimeOffset OccurredAtUtc,
    string CorrelationId,
    IReadOnlyDictionary<string, string> Metadata);
