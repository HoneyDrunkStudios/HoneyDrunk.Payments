namespace HoneyDrunk.Payments.Abstractions;

/// <summary>
/// Signed provider webhook event snapshot.
/// </summary>
/// <param name="Provider">Payment provider name.</param>
/// <param name="ProviderEventId">Provider webhook event identifier.</param>
/// <param name="EventType">Provider webhook event type.</param>
/// <param name="CreatedAt">Provider webhook event creation time in UTC.</param>
/// <param name="Livemode">Whether the provider event came from a live-mode account.</param>
/// <param name="ObjectId">Provider object identifier associated with the event, when available.</param>
/// <param name="ObjectType">Provider object type associated with the event, when available.</param>
/// <param name="Metadata">Additional provider metadata normalized as non-secret key/value pairs.</param>
public sealed record PaymentWebhookEventSnapshot(
    string Provider,
    string ProviderEventId,
    string EventType,
    DateTime CreatedAt,
    bool Livemode,
    string? ObjectId,
    string? ObjectType,
    IReadOnlyDictionary<string, string> Metadata);
