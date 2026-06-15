namespace HoneyDrunk.Payments.Abstractions;

/// <summary>
/// Validates and normalizes signed provider webhook events.
/// </summary>
public interface IPaymentWebhookEventValidator
{
    /// <summary>
    /// Validates a provider webhook signature and returns a normalized event snapshot.
    /// </summary>
    /// <param name="payload">Raw UTF-8 request body.</param>
    /// <param name="signatureHeader">Provider signature header value.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The normalized webhook event.</returns>
    ValueTask<PaymentWebhookEventSnapshot> ValidateWebhookEventAsync(
        string payload,
        string signatureHeader,
        CancellationToken cancellationToken);
}
