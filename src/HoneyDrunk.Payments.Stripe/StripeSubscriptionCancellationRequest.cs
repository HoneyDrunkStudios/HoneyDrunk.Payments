namespace HoneyDrunk.Payments.Stripe;

/// <summary>
/// Request for canceling a Stripe subscription.
/// </summary>
/// <param name="SubscriptionId">Stripe subscription identifier.</param>
/// <param name="InvoiceNow">Whether Stripe should invoice immediately.</param>
/// <param name="Prorate">Whether Stripe should prorate the cancellation.</param>
/// <param name="Reason">Optional cancellation comment.</param>
/// <param name="IdempotencyKey">Optional Stripe idempotency key.</param>
public sealed record StripeSubscriptionCancellationRequest(
    string SubscriptionId,
    bool InvoiceNow = false,
    bool Prorate = false,
    string? Reason = null,
    string? IdempotencyKey = null);
