namespace HoneyDrunk.Payments.Stripe;

/// <summary>
/// Signed Stripe webhook event snapshot.
/// </summary>
/// <param name="EventId">Stripe event identifier.</param>
/// <param name="EventType">Stripe event type.</param>
/// <param name="CreatedAt">Stripe event creation timestamp.</param>
/// <param name="Livemode">Whether the event came from live mode.</param>
/// <param name="ObjectId">Nested Stripe object identifier, when available.</param>
/// <param name="ObjectType">Nested Stripe object type, when available.</param>
/// <param name="Metadata">Nested Stripe object metadata, when available.</param>
public sealed record StripeWebhookEventSnapshot(
    string EventId,
    string EventType,
    DateTime CreatedAt,
    bool Livemode,
    string? ObjectId,
    string? ObjectType,
    IReadOnlyDictionary<string, string> Metadata);
