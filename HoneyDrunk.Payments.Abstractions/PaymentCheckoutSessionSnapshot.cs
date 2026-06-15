namespace HoneyDrunk.Payments.Abstractions;

/// <summary>
/// Hosted checkout session snapshot.
/// </summary>
/// <param name="Provider">Payment provider name.</param>
/// <param name="SessionId">Provider checkout session identifier.</param>
/// <param name="Url">Hosted checkout URL.</param>
/// <param name="TenantId">HoneyDrunk tenant identifier.</param>
/// <param name="ProjectId">Product project identifier.</param>
/// <param name="TierName">Product tier name.</param>
/// <param name="ProviderCustomerId">Provider customer identifier, when assigned.</param>
/// <param name="ProviderSubscriptionId">Provider subscription identifier, when assigned.</param>
public sealed record PaymentCheckoutSessionSnapshot(
    string Provider,
    string SessionId,
    string? Url,
    string TenantId,
    string ProjectId,
    string TierName,
    string? ProviderCustomerId,
    string? ProviderSubscriptionId);
