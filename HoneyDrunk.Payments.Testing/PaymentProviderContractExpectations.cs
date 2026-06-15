namespace HoneyDrunk.Payments.Testing;

/// <summary>
/// Expected provider values used by reusable payment provider contract tests.
/// </summary>
/// <param name="TenantId">HoneyDrunk tenant identifier expected in normalized provider snapshots.</param>
/// <param name="ProjectId">Product project identifier expected in normalized provider snapshots.</param>
/// <param name="TierName">Product tier name expected in normalized provider snapshots.</param>
/// <param name="ProviderPriceId">Provider recurring price identifier used for checkout creation.</param>
/// <param name="CustomerEmail">Customer email used when creating provider checkout sessions.</param>
/// <param name="ProviderCustomerId">Provider customer identifier expected in normalized snapshots.</param>
/// <param name="ProviderSubscriptionId">Provider subscription identifier expected in normalized snapshots.</param>
/// <param name="ProviderInvoiceId">Provider invoice identifier expected during invoice reconciliation.</param>
/// <param name="WebhookProviderEventId">Provider webhook event identifier expected after signature validation.</param>
/// <param name="WebhookEventType">Provider webhook event type expected after signature validation.</param>
/// <param name="WebhookPayload">Signed webhook payload supplied to the provider validator.</param>
/// <param name="WebhookSignatureHeader">Provider signature header supplied with the webhook payload.</param>
/// <param name="InvalidWebhookSignatureHeader">Invalid provider signature header expected to be rejected.</param>
public sealed record PaymentProviderContractExpectations(
    string TenantId,
    string ProjectId,
    string TierName,
    string ProviderPriceId,
    string CustomerEmail,
    string ProviderCustomerId,
    string ProviderSubscriptionId,
    string ProviderInvoiceId,
    string WebhookProviderEventId,
    string WebhookEventType,
    string WebhookPayload,
    string WebhookSignatureHeader,
    string InvalidWebhookSignatureHeader);
