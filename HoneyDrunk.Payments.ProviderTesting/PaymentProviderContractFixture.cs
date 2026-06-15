using HoneyDrunk.Kernel.Abstractions.Identity;
using HoneyDrunk.Kernel.Abstractions.Tenancy;
using HoneyDrunk.Payments.Abstractions;

namespace HoneyDrunk.Payments.ProviderTesting;

public abstract class PaymentProviderContractFixture
{
    protected PaymentProviderContractFixture(PaymentProviderContractExpectations expectations)
    {
        Expectations = expectations ?? throw new ArgumentNullException(nameof(expectations));
    }

    public PaymentProviderContractExpectations Expectations { get; }

    public abstract string ProviderName { get; }

    public abstract IPaymentSubscriptionLifecycleClient SubscriptionLifecycleClient { get; }

    public abstract IPaymentWebhookEventValidator WebhookEventValidator { get; }

    public abstract IPaymentInvoiceReconciliationClient InvoiceReconciliationClient { get; }

    public abstract IBillingEventEmitter BillingEventEmitter { get; }

    public virtual PaymentCheckoutSessionRequest CreateCheckoutRequest() =>
        new(
            Expectations.TenantId,
            Expectations.ProjectId,
            Expectations.TierName,
            Expectations.ProviderPriceId,
            "https://payments.test/success",
            "https://payments.test/cancel",
            "contract-checkout-1",
            CustomerEmail: Expectations.CustomerEmail,
            Metadata: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["contract_test"] = "checkout",
            });

    public virtual PaymentSubscriptionCancellationRequest CreateCancellationRequest() =>
        new(
            Expectations.ProviderSubscriptionId,
            InvoiceNow: true,
            Prorate: false,
            Reason: "contract test",
            IdempotencyKey: "contract-cancel-1");

    public virtual BillingEvent CreateBillingEvent(IReadOnlyDictionary<string, string> attributes) =>
        new(
            new TenantId(Expectations.TenantId),
            "payments.contract",
            "usage",
            Units: 1,
            new DateTimeOffset(2026, 6, 14, 12, 30, 0, TimeSpan.Zero),
            "contract-correlation-1",
            attributes);
}
