using HoneyDrunk.Payments.Abstractions;

namespace HoneyDrunk.Payments.Abstractions.Tests.Unit;

public sealed class ContractSurfaceTests
{
    [Fact]
    public void PublicSurfaceIncludesExpectedContractTypes()
    {
        Type[] expected =
        [
            typeof(IPaymentInvoiceReconciliationClient),
            typeof(IPaymentSubscriptionLifecycleClient),
            typeof(IPaymentWebhookEventValidator),
            typeof(PaymentCheckoutSessionRequest),
            typeof(PaymentCheckoutSessionSnapshot),
            typeof(PaymentInvoiceReconciliationSnapshot),
            typeof(PaymentProviderNames),
            typeof(PaymentSubscriptionCancellationRequest),
            typeof(PaymentSubscriptionSnapshot),
            typeof(PaymentWebhookEventSnapshot),
        ];

        var publicTypes = typeof(IPaymentSubscriptionLifecycleClient).Assembly.GetExportedTypes();

        foreach (var type in expected)
        {
            Assert.Contains(type, publicTypes);
        }
    }

    [Fact]
    public void CheckoutRequestCarriesProviderNeutralIdempotencyKey()
    {
        var request = new PaymentCheckoutSessionRequest(
            "tenant-1",
            "project-1",
            "Starter",
            "price_1",
            "https://payments.test/success",
            "https://payments.test/cancel",
            "checkout-1",
            CustomerEmail: "billing@example.com");

        Assert.Equal("checkout-1", request.IdempotencyKey);
    }
}
