namespace HoneyDrunk.Payments.Abstractions;

/// <summary>
/// Request for canceling a provider subscription.
/// </summary>
/// <param name="ProviderSubscriptionId">Provider subscription identifier.</param>
/// <param name="InvoiceNow">Whether the provider should invoice immediately.</param>
/// <param name="Prorate">Whether the provider should prorate the cancellation.</param>
/// <param name="Reason">Optional cancellation comment.</param>
/// <param name="IdempotencyKey">Optional provider idempotency key.</param>
public sealed record PaymentSubscriptionCancellationRequest(
    string ProviderSubscriptionId,
    bool InvoiceNow = false,
    bool Prorate = false,
    string? Reason = null,
    string? IdempotencyKey = null);
