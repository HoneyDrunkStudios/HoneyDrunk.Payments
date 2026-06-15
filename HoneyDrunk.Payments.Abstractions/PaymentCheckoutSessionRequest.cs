namespace HoneyDrunk.Payments.Abstractions;

/// <summary>
/// Request for creating a hosted subscription checkout session.
/// </summary>
/// <param name="TenantId">HoneyDrunk tenant identifier.</param>
/// <param name="ProjectId">Product project identifier.</param>
/// <param name="TierName">Product tier name.</param>
/// <param name="ProviderPriceId">Provider recurring price identifier.</param>
/// <param name="SuccessUrl">Checkout success redirect URL.</param>
/// <param name="CancelUrl">Checkout cancel redirect URL.</param>
/// <param name="IdempotencyKey">Provider idempotency key for retry-safe checkout creation.</param>
/// <param name="ProviderCustomerId">Existing provider customer identifier, when known.</param>
/// <param name="CustomerEmail">Customer email for provider-created customers.</param>
/// <param name="Quantity">Subscription item quantity.</param>
/// <param name="Metadata">Additional non-PII metadata.</param>
public sealed record PaymentCheckoutSessionRequest(
    string TenantId,
    string ProjectId,
    string TierName,
    string ProviderPriceId,
    string SuccessUrl,
    string CancelUrl,
    string IdempotencyKey,
    string? ProviderCustomerId = null,
    string? CustomerEmail = null,
    long Quantity = 1,
    IReadOnlyDictionary<string, string>? Metadata = null);
