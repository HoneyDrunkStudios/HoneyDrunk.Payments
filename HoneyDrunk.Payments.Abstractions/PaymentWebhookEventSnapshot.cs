namespace HoneyDrunk.Payments.Abstractions;

/// <summary>
/// Signed provider webhook event snapshot.
/// </summary>
public sealed record PaymentWebhookEventSnapshot(
    string Provider,
    string ProviderEventId,
    string EventType,
    DateTime CreatedAt,
    bool Livemode,
    string? ObjectId,
    string? ObjectType,
    IReadOnlyDictionary<string, string> Metadata);
