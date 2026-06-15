namespace HoneyDrunk.Payments.ProviderTesting;

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
    string WebhookSignatureHeader);
