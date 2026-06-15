using HoneyDrunk.Kernel.Abstractions.Identity;
using HoneyDrunk.Kernel.Abstractions.Tenancy;
using HoneyDrunk.Payments.Abstractions;

namespace HoneyDrunk.Payments.Tests.Unit;

/// <summary>
/// Base fixture for running provider-neutral payment contract tests against a provider implementation.
/// </summary>
public abstract class PaymentProviderContractFixture
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PaymentProviderContractFixture"/> class.
    /// </summary>
    /// <param name="expectations">Expected provider values used by the contract test assertions.</param>
    protected PaymentProviderContractFixture(PaymentProviderContractExpectations expectations)
    {
        Expectations = expectations ?? throw new ArgumentNullException(nameof(expectations));
    }

    /// <summary>
    /// Gets the expected provider values used by the contract test assertions.
    /// </summary>
    public PaymentProviderContractExpectations Expectations { get; }

    /// <summary>
    /// Gets the normalized provider name expected on provider-neutral snapshots.
    /// </summary>
    public abstract string ProviderName { get; }

    /// <summary>
    /// Gets the provider subscription lifecycle client under test.
    /// </summary>
    public abstract IPaymentSubscriptionLifecycleClient SubscriptionLifecycleClient { get; }

    /// <summary>
    /// Gets the provider webhook event validator under test.
    /// </summary>
    public abstract IPaymentWebhookEventValidator WebhookEventValidator { get; }

    /// <summary>
    /// Gets the provider invoice reconciliation client under test.
    /// </summary>
    public abstract IPaymentInvoiceReconciliationClient InvoiceReconciliationClient { get; }

    /// <summary>
    /// Gets the provider billing event emitter under test.
    /// </summary>
    public abstract IBillingEventEmitter BillingEventEmitter { get; }

    /// <summary>
    /// Creates the checkout session request used by the lifecycle contract test.
    /// </summary>
    /// <returns>A provider-neutral checkout session request.</returns>
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

    /// <summary>
    /// Creates the subscription cancellation request used by the lifecycle contract test.
    /// </summary>
    /// <returns>A provider-neutral subscription cancellation request.</returns>
    public virtual PaymentSubscriptionCancellationRequest CreateCancellationRequest() =>
        new(
            Expectations.ProviderSubscriptionId,
            InvoiceNow: true,
            Prorate: false,
            Reason: "contract test",
            IdempotencyKey: "contract-cancel-1");

    /// <summary>
    /// Creates the billing event used by the provider meter-event contract tests.
    /// </summary>
    /// <param name="attributes">Billing event attributes supplied to the provider emitter.</param>
    /// <returns>A provider-neutral billing event.</returns>
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
