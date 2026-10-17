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
    public void SubscriptionLifecycleClientContractExposesExpectedMethods()
    {
        AssertMethod(
            typeof(IPaymentSubscriptionLifecycleClient),
            nameof(IPaymentSubscriptionLifecycleClient.CreateCheckoutSessionAsync),
            typeof(ValueTask<PaymentCheckoutSessionSnapshot>),
            [typeof(PaymentCheckoutSessionRequest), typeof(CancellationToken)],
            [false, true]);

        AssertMethod(
            typeof(IPaymentSubscriptionLifecycleClient),
            nameof(IPaymentSubscriptionLifecycleClient.GetSubscriptionAsync),
            typeof(ValueTask<PaymentSubscriptionSnapshot>),
            [typeof(string), typeof(CancellationToken)],
            [false, true]);

        AssertMethod(
            typeof(IPaymentSubscriptionLifecycleClient),
            nameof(IPaymentSubscriptionLifecycleClient.CancelSubscriptionAsync),
            typeof(ValueTask<PaymentSubscriptionSnapshot>),
            [typeof(PaymentSubscriptionCancellationRequest), typeof(CancellationToken)],
            [false, true]);
    }

    [Fact]
    public void WebhookAndInvoiceContractsExposeExpectedMethods()
    {
        AssertMethod(
            typeof(IPaymentWebhookEventValidator),
            nameof(IPaymentWebhookEventValidator.ValidateWebhookEventAsync),
            typeof(ValueTask<PaymentWebhookEventSnapshot>),
            [typeof(string), typeof(string), typeof(CancellationToken)],
            [false, false, false]);

        AssertMethod(
            typeof(IPaymentInvoiceReconciliationClient),
            nameof(IPaymentInvoiceReconciliationClient.ReconcileInvoiceAsync),
            typeof(ValueTask<PaymentInvoiceReconciliationSnapshot>),
            [typeof(string), typeof(CancellationToken)],
            [false, true]);
    }

    [Fact]
    public void ProviderNeutralRecordShapesMatchPublishedContract()
    {
        AssertRecordShape(
            typeof(PaymentCheckoutSessionRequest),
            [
                (nameof(PaymentCheckoutSessionRequest.TenantId), typeof(string)),
                (nameof(PaymentCheckoutSessionRequest.ProjectId), typeof(string)),
                (nameof(PaymentCheckoutSessionRequest.TierName), typeof(string)),
                (nameof(PaymentCheckoutSessionRequest.ProviderPriceId), typeof(string)),
                (nameof(PaymentCheckoutSessionRequest.SuccessUrl), typeof(string)),
                (nameof(PaymentCheckoutSessionRequest.CancelUrl), typeof(string)),
                (nameof(PaymentCheckoutSessionRequest.IdempotencyKey), typeof(string)),
                (nameof(PaymentCheckoutSessionRequest.ProviderCustomerId), typeof(string)),
                (nameof(PaymentCheckoutSessionRequest.CustomerEmail), typeof(string)),
                (nameof(PaymentCheckoutSessionRequest.Quantity), typeof(long)),
                (nameof(PaymentCheckoutSessionRequest.Metadata), typeof(IReadOnlyDictionary<string, string>)),
            ]);

        AssertRecordShape(
            typeof(PaymentCheckoutSessionSnapshot),
            [
                (nameof(PaymentCheckoutSessionSnapshot.Provider), typeof(string)),
                (nameof(PaymentCheckoutSessionSnapshot.SessionId), typeof(string)),
                (nameof(PaymentCheckoutSessionSnapshot.Url), typeof(string)),
                (nameof(PaymentCheckoutSessionSnapshot.TenantId), typeof(string)),
                (nameof(PaymentCheckoutSessionSnapshot.ProjectId), typeof(string)),
                (nameof(PaymentCheckoutSessionSnapshot.TierName), typeof(string)),
                (nameof(PaymentCheckoutSessionSnapshot.ProviderCustomerId), typeof(string)),
                (nameof(PaymentCheckoutSessionSnapshot.ProviderSubscriptionId), typeof(string)),
            ]);

        AssertRecordShape(
            typeof(PaymentSubscriptionCancellationRequest),
            [
                (nameof(PaymentSubscriptionCancellationRequest.ProviderSubscriptionId), typeof(string)),
                (nameof(PaymentSubscriptionCancellationRequest.InvoiceNow), typeof(bool)),
                (nameof(PaymentSubscriptionCancellationRequest.Prorate), typeof(bool)),
                (nameof(PaymentSubscriptionCancellationRequest.Reason), typeof(string)),
                (nameof(PaymentSubscriptionCancellationRequest.IdempotencyKey), typeof(string)),
            ]);

        AssertRecordShape(
            typeof(PaymentSubscriptionSnapshot),
            [
                (nameof(PaymentSubscriptionSnapshot.Provider), typeof(string)),
                (nameof(PaymentSubscriptionSnapshot.ProviderSubscriptionId), typeof(string)),
                (nameof(PaymentSubscriptionSnapshot.ProviderCustomerId), typeof(string)),
                (nameof(PaymentSubscriptionSnapshot.Status), typeof(string)),
                (nameof(PaymentSubscriptionSnapshot.TenantId), typeof(string)),
                (nameof(PaymentSubscriptionSnapshot.ProjectId), typeof(string)),
                (nameof(PaymentSubscriptionSnapshot.TierName), typeof(string)),
                (nameof(PaymentSubscriptionSnapshot.CancelAtPeriodEnd), typeof(bool)),
                (nameof(PaymentSubscriptionSnapshot.CanceledAt), typeof(DateTime?)),
                (nameof(PaymentSubscriptionSnapshot.CreatedAt), typeof(DateTime)),
                (nameof(PaymentSubscriptionSnapshot.LatestInvoiceId), typeof(string)),
                (nameof(PaymentSubscriptionSnapshot.Metadata), typeof(IReadOnlyDictionary<string, string>)),
            ]);

        AssertRecordShape(
            typeof(PaymentInvoiceReconciliationSnapshot),
            [
                (nameof(PaymentInvoiceReconciliationSnapshot.Provider), typeof(string)),
                (nameof(PaymentInvoiceReconciliationSnapshot.ProviderInvoiceId), typeof(string)),
                (nameof(PaymentInvoiceReconciliationSnapshot.ProviderCustomerId), typeof(string)),
                (nameof(PaymentInvoiceReconciliationSnapshot.ProviderSubscriptionId), typeof(string)),
                (nameof(PaymentInvoiceReconciliationSnapshot.Status), typeof(string)),
                (nameof(PaymentInvoiceReconciliationSnapshot.Currency), typeof(string)),
                (nameof(PaymentInvoiceReconciliationSnapshot.AmountDue), typeof(long)),
                (nameof(PaymentInvoiceReconciliationSnapshot.AmountPaid), typeof(long)),
                (nameof(PaymentInvoiceReconciliationSnapshot.AmountRemaining), typeof(long)),
                (nameof(PaymentInvoiceReconciliationSnapshot.PeriodStart), typeof(DateTime)),
                (nameof(PaymentInvoiceReconciliationSnapshot.PeriodEnd), typeof(DateTime)),
                (nameof(PaymentInvoiceReconciliationSnapshot.PaidAt), typeof(DateTime?)),
                (nameof(PaymentInvoiceReconciliationSnapshot.HostedInvoiceUrl), typeof(string)),
                (nameof(PaymentInvoiceReconciliationSnapshot.InvoicePdfUrl), typeof(string)),
                (nameof(PaymentInvoiceReconciliationSnapshot.TenantId), typeof(string)),
                (nameof(PaymentInvoiceReconciliationSnapshot.ProjectId), typeof(string)),
                (nameof(PaymentInvoiceReconciliationSnapshot.TierName), typeof(string)),
                (nameof(PaymentInvoiceReconciliationSnapshot.Metadata), typeof(IReadOnlyDictionary<string, string>)),
            ]);

        AssertRecordShape(
            typeof(PaymentWebhookEventSnapshot),
            [
                (nameof(PaymentWebhookEventSnapshot.Provider), typeof(string)),
                (nameof(PaymentWebhookEventSnapshot.ProviderEventId), typeof(string)),
                (nameof(PaymentWebhookEventSnapshot.EventType), typeof(string)),
                (nameof(PaymentWebhookEventSnapshot.CreatedAt), typeof(DateTime)),
                (nameof(PaymentWebhookEventSnapshot.Livemode), typeof(bool)),
                (nameof(PaymentWebhookEventSnapshot.ObjectId), typeof(string)),
                (nameof(PaymentWebhookEventSnapshot.ObjectType), typeof(string)),
                (nameof(PaymentWebhookEventSnapshot.Metadata), typeof(IReadOnlyDictionary<string, string>)),
            ]);
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

    private static void AssertMethod(
        Type contractType,
        string methodName,
        Type expectedReturnType,
        Type[] expectedParameterTypes,
        bool[] expectedOptionalParameters)
    {
        var method = Assert.Single(contractType.GetMethods(), method => method.Name == methodName);
        Assert.Equal(expectedReturnType, method.ReturnType);

        var parameters = method.GetParameters();
        Assert.Equal(expectedParameterTypes.Length, parameters.Length);
        Assert.Equal(expectedOptionalParameters.Length, parameters.Length);

        for (var index = 0; index < parameters.Length; index++)
        {
            Assert.Equal(expectedParameterTypes[index], parameters[index].ParameterType);
            Assert.Equal(expectedOptionalParameters[index], parameters[index].IsOptional);
        }
    }

    private static void AssertRecordShape(Type recordType, (string name, Type type)[] expectedProperties)
    {
        AssertPrimaryConstructor(recordType, expectedProperties.Select(property => property.type).ToArray());

        foreach (var (propertyName, expectedType) in expectedProperties)
        {
            var property = recordType.GetProperty(propertyName);
            Assert.NotNull(property);
            Assert.Equal(expectedType, property.PropertyType);
        }
    }

    private static void AssertPrimaryConstructor(Type recordType, Type[] expectedParameterTypes)
    {
        var constructor = Assert.Single(
            recordType.GetConstructors(),
            candidate => candidate.GetParameters().Length == expectedParameterTypes.Length);

        var parameters = constructor.GetParameters();

        for (var index = 0; index < parameters.Length; index++)
        {
            Assert.Equal(expectedParameterTypes[index], parameters[index].ParameterType);
        }
    }
}
