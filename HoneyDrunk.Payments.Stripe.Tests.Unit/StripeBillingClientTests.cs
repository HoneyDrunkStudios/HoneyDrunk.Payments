using HoneyDrunk.Kernel.Abstractions.Identity;
using HoneyDrunk.Kernel.Abstractions.Tenancy;
using HoneyDrunk.Payments.Abstractions;
using Stripe;
using Stripe.Billing;
using StripeCheckout = Stripe.Checkout;

namespace HoneyDrunk.Payments.Stripe.Tests.Unit;

public sealed class StripeBillingClientTests
{
    [Fact]
    public void BillingClientPublicConstructorsDoNotAcceptRawApiKeyString()
    {
        var rawSecretConstructor = typeof(StripeBillingClient)
            .GetConstructors()
            .SingleOrDefault(constructor =>
            {
                var parameters = constructor.GetParameters();
                return parameters.Length == 1 && parameters[0].ParameterType == typeof(string);
            });

        Assert.Null(rawSecretConstructor);
    }

    [Fact]
    public void WebhookValidationContractsDoNotExposeRawSecretParameters()
    {
        var paymentValidatorParameters = typeof(IPaymentWebhookEventValidator)
            .GetMethods()
            .SelectMany(method => method.GetParameters());
        var stripeValidatorParameters = typeof(IStripeWebhookEventValidator)
            .GetMethods()
            .SelectMany(method => method.GetParameters());

        Assert.DoesNotContain(paymentValidatorParameters, parameter => parameter.Name == "webhookSecret");
        Assert.DoesNotContain(stripeValidatorParameters, parameter => parameter.Name == "webhookSecret");
    }

    [Fact]
    public async Task EmitAsyncRecordsKernelBillingEventAsStripeMeterEvent()
    {
        var buffer = new CapturingStripeMeterEventBuffer();
        var adapter = new StripeBillingEventEmitter(buffer);
        var tenantId = new TenantId("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        var occurredAtUtc = new DateTimeOffset(2026, 6, 14, 12, 30, 0, TimeSpan.Zero);
        var billingEvent = new BillingEvent(
            tenantId,
            "payments.submission.accepted",
            "sms",
            Units: 2,
            occurredAtUtc,
            "corr-1",
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [StripeBillingEventEmitter.BillingEventIdAttributeKey] = "bill-event-1",
                [StripeBillingEventEmitter.ProviderCustomerIdAttributeKey] = "cus_test",
                ["project_id"] = "project-1",
            });

        await adapter.EmitAsync(billingEvent, CancellationToken.None);

        Assert.NotNull(buffer.LastEvent);
        Assert.Equal("payments.submission.accepted.sms", buffer.LastEvent.EventName);
        Assert.Equal("cus_test", buffer.LastEvent.CustomerKey);
        Assert.Equal(2, buffer.LastEvent.Units);
        Assert.Equal(occurredAtUtc, buffer.LastEvent.OccurredAtUtc);
        Assert.Equal("bill-event-1", buffer.LastEvent.IdempotencyKey);
        Assert.Equal("corr-1", buffer.LastEvent.CorrelationId);
        Assert.Equal("project-1", buffer.LastEvent.Metadata["project_id"]);
        Assert.False(buffer.LastEvent.Metadata.ContainsKey(StripeBillingEventEmitter.BillingEventIdAttributeKey));
        Assert.False(buffer.LastEvent.Metadata.ContainsKey(StripeBillingEventEmitter.ProviderCustomerIdAttributeKey));
    }

    [Fact]
    public async Task EmitAsyncSkipsInternalTenantEvents()
    {
        var buffer = new CapturingStripeMeterEventBuffer();
        var adapter = new StripeBillingEventEmitter(buffer);
        var billingEvent = new BillingEvent(
            TenantId.Internal,
            "payments.submission.accepted",
            "email",
            Units: 1,
            DateTimeOffset.UtcNow,
            "corr-1",
            new Dictionary<string, string>(StringComparer.Ordinal));

        await adapter.EmitAsync(billingEvent, CancellationToken.None);

        Assert.Null(buffer.LastEvent);
    }

    [Fact]
    public async Task EmitAsyncRejectsNonPositiveUnits()
    {
        var adapter = new StripeBillingEventEmitter(new CapturingStripeMeterEventBuffer());
        var billingEvent = new BillingEvent(
            new TenantId("01ARZ3NDEKTSV4RRFFQ69G5FAV"),
            "payments.submission.accepted",
            "email",
            Units: 0,
            DateTimeOffset.UtcNow,
            "corr-1",
            new Dictionary<string, string>(StringComparer.Ordinal));

        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await adapter.EmitAsync(billingEvent, CancellationToken.None));

        Assert.Equal("units", exception.ParamName);
    }

    [Fact]
    public async Task EmitAsyncRequiresPerEventIdempotencyAttribute()
    {
        var adapter = new StripeBillingEventEmitter(new CapturingStripeMeterEventBuffer());
        var billingEvent = new BillingEvent(
            new TenantId("01ARZ3NDEKTSV4RRFFQ69G5FAV"),
            "payments.submission.accepted",
            "email",
            Units: 1,
            DateTimeOffset.UtcNow,
            "corr-1",
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [StripeBillingEventEmitter.ProviderCustomerIdAttributeKey] = "cus_test",
            });

        var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
            await adapter.EmitAsync(billingEvent, CancellationToken.None));

        Assert.Equal("billingEvent", exception.ParamName);
        Assert.Contains(StripeBillingEventEmitter.BillingEventIdAttributeKey, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EmitAsyncRequiresProviderCustomerIdAttribute()
    {
        var adapter = new StripeBillingEventEmitter(new CapturingStripeMeterEventBuffer());
        var billingEvent = new BillingEvent(
            new TenantId("01ARZ3NDEKTSV4RRFFQ69G5FAV"),
            "payments.submission.accepted",
            "email",
            Units: 1,
            DateTimeOffset.UtcNow,
            "corr-1",
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [StripeBillingEventEmitter.BillingEventIdAttributeKey] = "bill-event-1",
            });

        var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
            await adapter.EmitAsync(billingEvent, CancellationToken.None));

        Assert.Equal("billingEvent", exception.ParamName);
        Assert.Contains(StripeBillingEventEmitter.ProviderCustomerIdAttributeKey, exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("tok_test_123")]
    [InlineData("billing@example.com")]
    public async Task EmitAsyncRejectsSensitiveProviderCustomerIdAttribute(string providerCustomerId)
    {
        var adapter = new StripeBillingEventEmitter(new CapturingStripeMeterEventBuffer());
        var billingEvent = new BillingEvent(
            new TenantId("01ARZ3NDEKTSV4RRFFQ69G5FAV"),
            "payments.submission.accepted",
            "email",
            Units: 1,
            DateTimeOffset.UtcNow,
            "corr-1",
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [StripeBillingEventEmitter.BillingEventIdAttributeKey] = "bill-event-1",
                [StripeBillingEventEmitter.ProviderCustomerIdAttributeKey] = providerCustomerId,
            });

        var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
            await adapter.EmitAsync(billingEvent, CancellationToken.None));

        Assert.Equal("billingEvent.Attributes", exception.ParamName);
        Assert.Contains("sensitive", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("whsec_test")]
    [InlineData("billing@example.com")]
    public async Task EmitAsyncRejectsSensitiveBillingEventIdAttribute(string billingEventId)
    {
        var adapter = new StripeBillingEventEmitter(new CapturingStripeMeterEventBuffer());
        var billingEvent = new BillingEvent(
            new TenantId("01ARZ3NDEKTSV4RRFFQ69G5FAV"),
            "payments.submission.accepted",
            "email",
            Units: 1,
            DateTimeOffset.UtcNow,
            "corr-1",
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [StripeBillingEventEmitter.BillingEventIdAttributeKey] = billingEventId,
                [StripeBillingEventEmitter.ProviderCustomerIdAttributeKey] = "cus_test",
            });

        var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
            await adapter.EmitAsync(billingEvent, CancellationToken.None));

        Assert.Equal("billingEvent.Attributes", exception.ParamName);
        Assert.Contains("sensitive", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("contains-token-fragment")]
    [InlineData("billing@example.com")]
    public async Task EmitAsyncRejectsSensitiveBillingCorrelationId(string correlationId)
    {
        var adapter = new StripeBillingEventEmitter(new CapturingStripeMeterEventBuffer());
        var billingEvent = new BillingEvent(
            new TenantId("01ARZ3NDEKTSV4RRFFQ69G5FAV"),
            "payments.submission.accepted",
            "email",
            Units: 1,
            DateTimeOffset.UtcNow,
            correlationId,
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [StripeBillingEventEmitter.BillingEventIdAttributeKey] = "bill-event-1",
                [StripeBillingEventEmitter.ProviderCustomerIdAttributeKey] = "cus_test",
            });

        var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
            await adapter.EmitAsync(billingEvent, CancellationToken.None));

        Assert.Equal("billingEvent.CorrelationId", exception.ParamName);
        Assert.Contains("sensitive", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EmitAsyncRejectsSensitiveBillingAttributeKeys()
    {
        var adapter = new StripeBillingEventEmitter(new CapturingStripeMeterEventBuffer());
        var billingEvent = new BillingEvent(
            new TenantId("01ARZ3NDEKTSV4RRFFQ69G5FAV"),
            "payments.submission.accepted",
            "email",
            Units: 1,
            DateTimeOffset.UtcNow,
            "corr-1",
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [StripeBillingEventEmitter.BillingEventIdAttributeKey] = "bill-event-1",
                [StripeBillingEventEmitter.ProviderCustomerIdAttributeKey] = "cus_test",
                ["api_token"] = "secret",
            });

        var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
            await adapter.EmitAsync(billingEvent, CancellationToken.None));

        Assert.Equal("billingEvent.Attributes", exception.ParamName);
        Assert.Contains("sensitive", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("tok_test_123")]
    [InlineData("billing@example.com")]
    public async Task EmitAsyncRejectsSensitiveBillingAttributeValues(string sensitiveValue)
    {
        var adapter = new StripeBillingEventEmitter(new CapturingStripeMeterEventBuffer());
        var billingEvent = new BillingEvent(
            new TenantId("01ARZ3NDEKTSV4RRFFQ69G5FAV"),
            "payments.submission.accepted",
            "email",
            Units: 1,
            DateTimeOffset.UtcNow,
            "corr-1",
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [StripeBillingEventEmitter.BillingEventIdAttributeKey] = "bill-event-1",
                [StripeBillingEventEmitter.ProviderCustomerIdAttributeKey] = "cus_test",
                ["provider_reference"] = sensitiveValue,
            });

        var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
            await adapter.EmitAsync(billingEvent, CancellationToken.None));

        Assert.Equal("billingEvent.Attributes", exception.ParamName);
        Assert.Contains("sensitive", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MeterEventReplayDispatcherSendsBufferedEventToStripeClient()
    {
        var client = new CapturingStripeMeteredBillingClient();
        var dispatcher = new StripeMeterEventReplayDispatcher(client);
        var meterEvent = new StripeMeterEvent(
            "payments.submission.accepted.email",
            "cus_test",
            1,
            DateTimeOffset.UtcNow,
            "bill-event-1",
            "corr-1",
            new Dictionary<string, string>(StringComparer.Ordinal));

        await dispatcher.DispatchAsync(meterEvent, CancellationToken.None);

        Assert.Same(meterEvent, client.LastEvent);
    }

    [Fact]
    public async Task BillingClientRecordsMeterEventsThroughStripeSdkPayload()
    {
        var sdk = new CapturingStripeBillingSdk();
        var client = new StripeBillingClient(sdk, timeProvider: new FixedTimeProvider(new DateTimeOffset(2026, 6, 15, 0, 0, 0, TimeSpan.Zero)));
        var occurredAtUtc = new DateTimeOffset(2026, 6, 14, 12, 30, 0, TimeSpan.Zero);
        var meterEvent = new StripeMeterEvent(
            "payments.submission.accepted.email",
            "cus_test",
            3,
            occurredAtUtc,
            "bill-event-1",
            "corr-1",
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["project_id"] = "project-1",
            });

        await client.RecordMeterEventAsync(meterEvent, CancellationToken.None);

        Assert.NotNull(sdk.LastMeterEventOptions);
        Assert.Equal("payments.submission.accepted.email", sdk.LastMeterEventOptions.EventName);
        Assert.Equal("bill-event-1", sdk.LastMeterEventOptions.Identifier);
        Assert.Equal(occurredAtUtc.UtcDateTime, sdk.LastMeterEventOptions.Timestamp);
        Assert.Equal("bill-event-1", sdk.LastIdempotencyKey);
        Assert.Equal("cus_test", sdk.LastMeterEventOptions.Payload[StripeBillingClient.MeterCustomerPayloadKey]);
        Assert.Equal("3", sdk.LastMeterEventOptions.Payload[StripeBillingClient.MeterValuePayloadKey]);
        Assert.Equal("bill-event-1", sdk.LastMeterEventOptions.Payload[StripeBillingClient.MeterEventIdPayloadKey]);
        Assert.Equal("corr-1", sdk.LastMeterEventOptions.Payload[StripeBillingClient.MeterCorrelationPayloadKey]);
        Assert.Equal("project-1", sdk.LastMeterEventOptions.Payload["project_id"]);
    }

    [Fact]
    public async Task BillingClientRejectsNonPositiveMeterEventUnitsWithMemberParameterName()
    {
        var client = new StripeBillingClient(new CapturingStripeBillingSdk());
        var meterEvent = new StripeMeterEvent(
            "payments.submission.accepted.email",
            "cus_test",
            0,
            DateTimeOffset.UtcNow,
            "bill-event-1",
            "corr-1",
            new Dictionary<string, string>(StringComparer.Ordinal));

        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await client.RecordMeterEventAsync(meterEvent, CancellationToken.None));

        Assert.Equal("meterEvent.Units", exception.ParamName);
    }

    [Fact]
    public async Task BillingClientRejectsOversizedMeterMetadata()
    {
        var client = new StripeBillingClient(new CapturingStripeBillingSdk());
        var metadata = Enumerable
            .Range(0, StripeMetadataPolicy.MaxCustomMetadataEntries + 1)
            .ToDictionary(index => $"safe_key_{index}", index => "safe-value", StringComparer.Ordinal);
        var meterEvent = new StripeMeterEvent(
            "payments.submission.accepted.email",
            "01ARZ3NDEKTSV4RRFFQ69G5FAV",
            3,
            DateTimeOffset.UtcNow,
            "bill-event-1",
            "corr-1",
            metadata);

        var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
            await client.RecordMeterEventAsync(meterEvent, CancellationToken.None));

        Assert.Equal("meterEvent.Metadata", exception.ParamName);
        Assert.Contains("custom entries", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("tok_test_123")]
    [InlineData("billing@example.com")]
    public async Task BillingClientRejectsSensitiveMeterCustomerKey(string customerKey)
    {
        var client = new StripeBillingClient(new CapturingStripeBillingSdk());
        var meterEvent = new StripeMeterEvent(
            "payments.submission.accepted.email",
            customerKey,
            3,
            DateTimeOffset.UtcNow,
            "bill-event-1",
            "corr-1",
            new Dictionary<string, string>(StringComparer.Ordinal));

        var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
            await client.RecordMeterEventAsync(meterEvent, CancellationToken.None));

        Assert.Equal("meterEvent.CustomerKey", exception.ParamName);
        Assert.Contains("sensitive", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("meterEvent.IdempotencyKey", "cus_test", "sk_test_123", "corr-1")]
    [InlineData("meterEvent.CorrelationId", "cus_test", "bill-event-1", "contains-secret-fragment")]
    public async Task BillingClientRejectsSensitiveMeterIdentifiers(
        string expectedParameterName,
        string customerKey,
        string idempotencyKey,
        string correlationId)
    {
        var client = new StripeBillingClient(new CapturingStripeBillingSdk());
        var meterEvent = new StripeMeterEvent(
            "payments.submission.accepted.email",
            customerKey,
            3,
            DateTimeOffset.UtcNow,
            idempotencyKey,
            correlationId,
            new Dictionary<string, string>(StringComparer.Ordinal));

        var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
            await client.RecordMeterEventAsync(meterEvent, CancellationToken.None));

        Assert.Equal(expectedParameterName, exception.ParamName);
        Assert.Contains("sensitive", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BillingClientRejectsStaleMeterEventAsPermanentFailure()
    {
        var sdk = new CapturingStripeBillingSdk();
        var now = new DateTimeOffset(2026, 6, 15, 0, 0, 0, TimeSpan.Zero);
        var client = new StripeBillingClient(sdk, timeProvider: new FixedTimeProvider(now));
        var meterEvent = new StripeMeterEvent(
            "payments.submission.accepted.email",
            "cus_test",
            3,
            now.Subtract(StripeBillingClient.MaxMeterEventAge).AddSeconds(-1),
            "bill-event-1",
            "corr-1",
            new Dictionary<string, string>(StringComparer.Ordinal));

        var exception = await Assert.ThrowsAsync<StripeMeterEventPermanentFailureException>(async () =>
            await client.RecordMeterEventAsync(meterEvent, CancellationToken.None));

        Assert.Equal(StripeMeterEventPermanentFailureReason.TimestampTooOld, exception.Reason);
        Assert.Same(meterEvent, exception.MeterEvent);
        Assert.Null(sdk.LastMeterEventOptions);
    }

    [Fact]
    public async Task BillingClientRejectsFutureMeterEventAsPermanentFailure()
    {
        var sdk = new CapturingStripeBillingSdk();
        var now = new DateTimeOffset(2026, 6, 15, 0, 0, 0, TimeSpan.Zero);
        var client = new StripeBillingClient(sdk, timeProvider: new FixedTimeProvider(now));
        var meterEvent = new StripeMeterEvent(
            "payments.submission.accepted.email",
            "cus_test",
            3,
            now.Add(StripeBillingClient.MaxMeterEventFutureSkew).AddSeconds(1),
            "bill-event-1",
            "corr-1",
            new Dictionary<string, string>(StringComparer.Ordinal));

        var exception = await Assert.ThrowsAsync<StripeMeterEventPermanentFailureException>(async () =>
            await client.RecordMeterEventAsync(meterEvent, CancellationToken.None));

        Assert.Equal(StripeMeterEventPermanentFailureReason.TimestampTooNew, exception.Reason);
        Assert.Same(meterEvent, exception.MeterEvent);
        Assert.Null(sdk.LastMeterEventOptions);
    }

    [Fact]
    public async Task BillingClientCreatesCheckoutSessionWithPaymentsMetadata()
    {
        var sdk = new CapturingStripeBillingSdk
        {
            CheckoutSession = new StripeCheckout.Session
            {
                Id = "cs_test",
                Url = "https://checkout.stripe.test/session",
                CustomerId = "cus_test",
                SubscriptionId = "sub_test",
            },
        };
        var client = new StripeBillingClient(sdk);
        var request = new StripeCheckoutSessionRequest(
            "01ARZ3NDEKTSV4RRFFQ69G5FAV",
            "project-1",
            "Starter",
            "price_starter",
            "https://payments.test/success",
            "https://payments.test/cancel",
            "checkout-1",
            CustomerEmail: "billing@example.com",
            Metadata: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [StripeBillingClient.TenantMetadataKey] = "caller-tenant",
                [StripeBillingClient.ProjectMetadataKey] = "caller-project",
                [StripeBillingClient.TierMetadataKey] = "caller-tier",
            });

        var session = await client.CreateCheckoutSessionAsync(request);

        Assert.Equal("cs_test", session.SessionId);
        Assert.Equal("sub_test", session.StripeSubscriptionId);
        Assert.NotNull(sdk.LastCheckoutSessionOptions);
        Assert.Equal("subscription", sdk.LastCheckoutSessionOptions.Mode);
        Assert.NotNull(sdk.LastCheckoutSessionOptions.AutomaticTax);
        Assert.True(sdk.LastCheckoutSessionOptions.AutomaticTax.Enabled);
        Assert.Equal("billing@example.com", sdk.LastCheckoutSessionOptions.CustomerEmail);
        Assert.Equal("price_starter", Assert.Single(sdk.LastCheckoutSessionOptions.LineItems).Price);
        Assert.Equal("checkout-1", sdk.LastIdempotencyKey);
        Assert.Equal("01ARZ3NDEKTSV4RRFFQ69G5FAV", sdk.LastCheckoutSessionOptions.Metadata[StripeBillingClient.TenantMetadataKey]);
        Assert.Equal("project-1", sdk.LastCheckoutSessionOptions.SubscriptionData.Metadata[StripeBillingClient.ProjectMetadataKey]);
        Assert.Equal("Starter", sdk.LastCheckoutSessionOptions.SubscriptionData.Metadata[StripeBillingClient.TierMetadataKey]);
    }

    [Fact]
    public async Task BillingClientRejectsSensitiveCheckoutMetadataKeys()
    {
        var client = new StripeBillingClient(new CapturingStripeBillingSdk());
        var request = new StripeCheckoutSessionRequest(
            "01ARZ3NDEKTSV4RRFFQ69G5FAV",
            "project-1",
            "Starter",
            "price_starter",
            "https://payments.test/success",
            "https://payments.test/cancel",
            "checkout-1",
            CustomerEmail: "billing@example.com",
            Metadata: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["webhook_secret"] = "whsec_test",
            });

        var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
            await client.CreateCheckoutSessionAsync(request));

        Assert.Equal("request.Metadata", exception.ParamName);
        Assert.Contains("sensitive", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("request.StripeCustomerId", "price_starter", "checkout-1", "billing@example.com")]
    [InlineData("request.IdempotencyKey", "price_starter", "tok_test_123", null)]
    [InlineData("request.StripePriceId", "price_secret", "checkout-1", null)]
    public async Task BillingClientRejectsSensitiveCheckoutProviderIdentifiers(
        string expectedParameterName,
        string stripePriceId,
        string idempotencyKey,
        string? stripeCustomerId)
    {
        var client = new StripeBillingClient(new CapturingStripeBillingSdk());
        var request = new StripeCheckoutSessionRequest(
            "01ARZ3NDEKTSV4RRFFQ69G5FAV",
            "project-1",
            "Starter",
            stripePriceId,
            "https://payments.test/success",
            "https://payments.test/cancel",
            idempotencyKey,
            StripeCustomerId: stripeCustomerId,
            CustomerEmail: stripeCustomerId is null ? "billing@example.com" : null);

        var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
            await client.CreateCheckoutSessionAsync(request));

        Assert.Equal(expectedParameterName, exception.ParamName);
        Assert.Contains("sensitive", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("request.TenantId", "billing@example.com", "project-1", "Starter")]
    [InlineData("request.ProjectId", "01ARZ3NDEKTSV4RRFFQ69G5FAV", "sk_test_123", "Starter")]
    [InlineData("request.TierName", "01ARZ3NDEKTSV4RRFFQ69G5FAV", "project-1", "contains-secret-fragment")]
    public async Task BillingClientRejectsSensitiveReservedCheckoutMetadataValues(
        string expectedParameterName,
        string tenantId,
        string projectId,
        string tierName)
    {
        var client = new StripeBillingClient(new CapturingStripeBillingSdk());
        var request = new StripeCheckoutSessionRequest(
            tenantId,
            projectId,
            tierName,
            "price_starter",
            "https://payments.test/success",
            "https://payments.test/cancel",
            "checkout-1",
            CustomerEmail: "billing@example.com");

        var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
            await client.CreateCheckoutSessionAsync(request));

        Assert.Equal(expectedParameterName, exception.ParamName);
        Assert.Contains("sensitive", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BillingClientRejectsOversizedReservedCheckoutMetadataValue()
    {
        var client = new StripeBillingClient(new CapturingStripeBillingSdk());
        var request = new StripeCheckoutSessionRequest(
            "01ARZ3NDEKTSV4RRFFQ69G5FAV",
            "project-1",
            new string('a', StripeMetadataPolicy.MaxMetadataValueLength + 1),
            "price_starter",
            "https://payments.test/success",
            "https://payments.test/cancel",
            "checkout-1",
            CustomerEmail: "billing@example.com");

        var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
            await client.CreateCheckoutSessionAsync(request));

        Assert.Equal("request.TierName", exception.ParamName);
        Assert.Contains("cannot exceed", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BillingSdkPinsExpectedStripeApiVersion()
    {
        var sdk = new StripeBillingSdk(new FixedStripeApiKeyProvider("sk_test"));

        var options = await sdk.CreateRequestOptionsAsync("idem-1", CancellationToken.None);

        Assert.Equal("sk_test", options.ApiKey);
        Assert.Equal("idem-1", options.IdempotencyKey);
        Assert.Equal(StripeBillingSdk.StripeApiVersion, StripeConfiguration.ApiVersion);
        Assert.Equal("2026-05-27.dahlia", StripeBillingSdk.StripeApiVersion);
    }

    [Fact]
    public async Task BillingClientTreatsWhitespaceStripeCustomerIdAsMissing()
    {
        var sdk = new CapturingStripeBillingSdk
        {
            CheckoutSession = new StripeCheckout.Session
            {
                Id = "cs_test",
                Url = "https://checkout.stripe.test/session",
                CustomerId = "cus_test",
                SubscriptionId = "sub_test",
            },
        };
        var client = new StripeBillingClient(sdk);
        var request = new StripeCheckoutSessionRequest(
            "01ARZ3NDEKTSV4RRFFQ69G5FAV",
            "project-1",
            "Starter",
            "price_starter",
            "https://payments.test/success",
            "https://payments.test/cancel",
            "checkout-1",
            StripeCustomerId: " ",
            CustomerEmail: "billing@example.com");

        await client.CreateCheckoutSessionAsync(request);

        Assert.NotNull(sdk.LastCheckoutSessionOptions);
        Assert.Null(sdk.LastCheckoutSessionOptions.Customer);
        Assert.Equal("billing@example.com", sdk.LastCheckoutSessionOptions.CustomerEmail);
    }

    [Fact]
    public async Task BillingClientSupportsProviderNeutralCheckoutContract()
    {
        var sdk = new CapturingStripeBillingSdk
        {
            CheckoutSession = new StripeCheckout.Session
            {
                Id = "cs_test",
                Url = "https://checkout.stripe.test/session",
                CustomerId = "cus_test",
                SubscriptionId = "sub_test",
            },
        };
        IPaymentSubscriptionLifecycleClient client = new StripeBillingClient(sdk);
        var request = new PaymentCheckoutSessionRequest(
            "01ARZ3NDEKTSV4RRFFQ69G5FAV",
            "project-1",
            "Starter",
            "price_starter",
            "https://payments.test/success",
            "https://payments.test/cancel",
            "checkout-1",
            CustomerEmail: "billing@example.com");

        var session = await client.CreateCheckoutSessionAsync(request);

        Assert.Equal(PaymentProviderNames.Stripe, session.Provider);
        Assert.Equal("cs_test", session.SessionId);
        Assert.Equal("sub_test", session.ProviderSubscriptionId);
        Assert.Equal("cus_test", session.ProviderCustomerId);
        Assert.NotNull(sdk.LastCheckoutSessionOptions);
        Assert.Equal("price_starter", Assert.Single(sdk.LastCheckoutSessionOptions.LineItems).Price);
        Assert.Equal("checkout-1", sdk.LastIdempotencyKey);
    }

    [Fact]
    public async Task BillingClientRejectsCheckoutSessionWithoutIdempotencyKey()
    {
        var client = new StripeBillingClient(new CapturingStripeBillingSdk());
        var request = new StripeCheckoutSessionRequest(
            "01ARZ3NDEKTSV4RRFFQ69G5FAV",
            "project-1",
            "Starter",
            "price_starter",
            "https://payments.test/success",
            "https://payments.test/cancel",
            " ",
            CustomerEmail: "billing@example.com");

        var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
            await client.CreateCheckoutSessionAsync(request));

        Assert.Equal("request.IdempotencyKey", exception.ParamName);
    }

    [Fact]
    public async Task BillingClientRejectsNonPositiveCheckoutQuantityWithMemberParameterName()
    {
        var client = new StripeBillingClient(new CapturingStripeBillingSdk());
        var request = new StripeCheckoutSessionRequest(
            "01ARZ3NDEKTSV4RRFFQ69G5FAV",
            "project-1",
            "Starter",
            "price_starter",
            "https://payments.test/success",
            "https://payments.test/cancel",
            "checkout-1",
            CustomerEmail: "billing@example.com",
            Quantity: 0);

        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await client.CreateCheckoutSessionAsync(request));

        Assert.Equal("request.Quantity", exception.ParamName);
    }

    [Fact]
    public async Task BillingClientCancelsSubscriptionAndReturnsSnapshot()
    {
        var canceledAt = DateTime.UtcNow;
        var sdk = new CapturingStripeBillingSdk
        {
            Subscription = new Subscription
            {
                Id = "sub_test",
                CustomerId = "cus_test",
                Status = "canceled",
                CanceledAt = canceledAt,
                LatestInvoiceId = "in_test",
                Metadata = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    [StripeBillingClient.TenantMetadataKey] = "01ARZ3NDEKTSV4RRFFQ69G5FAV",
                    [StripeBillingClient.ProjectMetadataKey] = "project-1",
                    [StripeBillingClient.TierMetadataKey] = "Starter",
                },
            },
        };
        var client = new StripeBillingClient(sdk);

        var snapshot = await client.CancelSubscriptionAsync(
            new StripeSubscriptionCancellationRequest("sub_test", InvoiceNow: true, Prorate: true, Reason: "customer request", IdempotencyKey: "cancel-1"));

        Assert.Equal("sub_test", snapshot.SubscriptionId);
        Assert.Equal("canceled", snapshot.Status);
        Assert.Equal("project-1", snapshot.ProjectId);
        Assert.NotNull(sdk.LastSubscriptionCancelOptions);
        Assert.True(sdk.LastSubscriptionCancelOptions.InvoiceNow);
        Assert.True(sdk.LastSubscriptionCancelOptions.Prorate);
        Assert.Equal("customer request", sdk.LastSubscriptionCancelOptions.CancellationDetails.Comment);
        Assert.Equal("cancel-1", sdk.LastIdempotencyKey);
    }

    [Theory]
    [InlineData("request.SubscriptionId", "sub_secret", "cancel-1", "customer request")]
    [InlineData("request.IdempotencyKey", "sub_test", "whsec_test", "customer request")]
    [InlineData("request.Reason", "sub_test", "cancel-1", "email requested")]
    public async Task BillingClientRejectsSensitiveCancellationProviderIdentifiers(
        string expectedParameterName,
        string subscriptionId,
        string idempotencyKey,
        string reason)
    {
        var client = new StripeBillingClient(new CapturingStripeBillingSdk());
        var request = new StripeSubscriptionCancellationRequest(
            subscriptionId,
            InvoiceNow: true,
            Prorate: false,
            Reason: reason,
            IdempotencyKey: idempotencyKey);

        var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
            await client.CancelSubscriptionAsync(request));

        Assert.Equal(expectedParameterName, exception.ParamName);
        Assert.Contains("sensitive", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BillingClientGetsSubscriptionThroughProviderNeutralContract()
    {
        var createdAt = DateTime.UtcNow.AddDays(-7);
        var sdk = new CapturingStripeBillingSdk
        {
            Subscription = new Subscription
            {
                Id = "sub_test",
                CustomerId = "cus_test",
                Status = "active",
                Created = createdAt,
                LatestInvoiceId = "in_test",
                Metadata = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    [StripeBillingClient.TenantMetadataKey] = "01ARZ3NDEKTSV4RRFFQ69G5FAV",
                    [StripeBillingClient.ProjectMetadataKey] = "project-1",
                    [StripeBillingClient.TierMetadataKey] = "Starter",
                },
            },
        };
        IPaymentSubscriptionLifecycleClient client = new StripeBillingClient(sdk);

        var snapshot = await client.GetSubscriptionAsync("sub_test", CancellationToken.None);

        Assert.Equal("sub_test", sdk.LastSubscriptionId);
        Assert.Equal(PaymentProviderNames.Stripe, snapshot.Provider);
        Assert.Equal("sub_test", snapshot.ProviderSubscriptionId);
        Assert.Equal("cus_test", snapshot.ProviderCustomerId);
        Assert.Equal("active", snapshot.Status);
        Assert.Equal("project-1", snapshot.ProjectId);
        Assert.Equal(createdAt, snapshot.CreatedAt);
        Assert.Equal("in_test", snapshot.LatestInvoiceId);
    }

    [Theory]
    [InlineData("sk_test_123")]
    [InlineData("billing@example.com")]
    public async Task BillingClientRejectsSensitiveSubscriptionLookupId(string subscriptionId)
    {
        IPaymentSubscriptionLifecycleClient client = new StripeBillingClient(new CapturingStripeBillingSdk());

        var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
            await client.GetSubscriptionAsync(subscriptionId, CancellationToken.None));

        Assert.Equal("subscriptionId", exception.ParamName);
        Assert.Contains("sensitive", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BillingClientStripsSensitiveInboundSubscriptionMetadata()
    {
        var sdk = new CapturingStripeBillingSdk
        {
            Subscription = new Subscription
            {
                Id = "sub_test",
                CustomerId = "cus_test",
                Status = "active",
                Metadata = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    [StripeBillingClient.TenantMetadataKey] = "01ARZ3NDEKTSV4RRFFQ69G5FAV",
                    [StripeBillingClient.ProjectMetadataKey] = "billing@example.com",
                    [StripeBillingClient.TierMetadataKey] = "Starter",
                    ["api_token"] = "secret",
                    ["support_email"] = "billing@example.com",
                },
            },
        };
        IPaymentSubscriptionLifecycleClient client = new StripeBillingClient(sdk);

        var snapshot = await client.GetSubscriptionAsync("sub_test", CancellationToken.None);

        Assert.Null(snapshot.ProjectId);
        Assert.Equal("Starter", snapshot.TierName);
        Assert.False(snapshot.Metadata.ContainsKey(StripeBillingClient.ProjectMetadataKey));
        Assert.False(snapshot.Metadata.ContainsKey("api_token"));
        Assert.False(snapshot.Metadata.ContainsKey("support_email"));
    }

    [Fact]
    public async Task BillingClientCancelsSubscriptionThroughProviderNeutralContract()
    {
        var canceledAt = DateTime.UtcNow;
        var sdk = new CapturingStripeBillingSdk
        {
            Subscription = new Subscription
            {
                Id = "sub_test",
                CustomerId = "cus_test",
                Status = "canceled",
                CanceledAt = canceledAt,
                Metadata = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    [StripeBillingClient.TenantMetadataKey] = "01ARZ3NDEKTSV4RRFFQ69G5FAV",
                    [StripeBillingClient.ProjectMetadataKey] = "project-1",
                    [StripeBillingClient.TierMetadataKey] = "Starter",
                },
            },
        };
        IPaymentSubscriptionLifecycleClient client = new StripeBillingClient(sdk);

        var snapshot = await client.CancelSubscriptionAsync(
            new PaymentSubscriptionCancellationRequest("sub_test", InvoiceNow: true, Prorate: false, Reason: "duplicate", IdempotencyKey: "cancel-1"),
            CancellationToken.None);

        Assert.Equal("sub_test", sdk.LastSubscriptionId);
        Assert.Equal("cancel-1", sdk.LastIdempotencyKey);
        Assert.NotNull(sdk.LastSubscriptionCancelOptions);
        Assert.True(sdk.LastSubscriptionCancelOptions.InvoiceNow);
        Assert.False(sdk.LastSubscriptionCancelOptions.Prorate);
        Assert.Equal("duplicate", sdk.LastSubscriptionCancelOptions.CancellationDetails.Comment);
        Assert.Equal(PaymentProviderNames.Stripe, snapshot.Provider);
        Assert.Equal("sub_test", snapshot.ProviderSubscriptionId);
        Assert.Equal(canceledAt, snapshot.CanceledAt);
    }

    [Fact]
    public async Task BillingClientNormalizesWebhookEventMetadata()
    {
        var sdk = new CapturingStripeBillingSdk();
        var client = new StripeBillingClient(sdk, new FixedStripeWebhookSecretProvider("whsec_test"));
        const string Payload = """
            {
              "id": "evt_test",
              "object": "event",
              "created": 1710000000,
              "livemode": false,
              "type": "customer.subscription.updated",
              "data": {
                "object": {
                  "id": "sub_test",
                  "object": "subscription",
                  "metadata": {
                    "project_id": "project-1"
                  }
                }
              }
            }
            """;

        var snapshot = await client.ValidateWebhookEventAsync(Payload, "t=1,v1=test", CancellationToken.None);

        Assert.Equal("evt_test", snapshot.EventId);
        Assert.Equal("customer.subscription.updated", snapshot.EventType);
        Assert.Equal("sub_test", snapshot.ObjectId);
        Assert.Equal("subscription", snapshot.ObjectType);
        Assert.Equal("project-1", snapshot.Metadata["project_id"]);
        Assert.Equal(Payload, sdk.LastWebhookPayload);
        Assert.Equal("t=1,v1=test", sdk.LastWebhookSignature);
        Assert.Equal("whsec_test", sdk.LastWebhookSecret);
    }

    [Fact]
    public async Task BillingClientStripsSensitiveInboundWebhookMetadata()
    {
        var sdk = new CapturingStripeBillingSdk();
        IPaymentWebhookEventValidator client = new StripeBillingClient(sdk, new FixedStripeWebhookSecretProvider("whsec_test"));
        const string Payload = """
            {
              "id": "evt_test",
              "object": "event",
              "created": 1710000000,
              "livemode": false,
              "type": "customer.subscription.updated",
              "data": {
                "object": {
                  "id": "sub_test",
                  "object": "subscription",
                  "metadata": {
                    "project_id": "billing@example.com",
                    "tier_name": "Starter",
                    "webhook_secret": "whsec_test",
                    "customer_email": "billing@example.com"
                  }
                }
              }
            }
            """;

        var snapshot = await client.ValidateWebhookEventAsync(Payload, "t=1,v1=test", CancellationToken.None);

        Assert.Equal("Starter", snapshot.Metadata[StripeBillingClient.TierMetadataKey]);
        Assert.False(snapshot.Metadata.ContainsKey(StripeBillingClient.ProjectMetadataKey));
        Assert.False(snapshot.Metadata.ContainsKey("webhook_secret"));
        Assert.False(snapshot.Metadata.ContainsKey("customer_email"));
    }

    [Fact]
    public async Task BillingClientSupportsProviderNeutralWebhookContract()
    {
        var sdk = new CapturingStripeBillingSdk();
        IPaymentWebhookEventValidator client = new StripeBillingClient(sdk, new FixedStripeWebhookSecretProvider("whsec_test"));
        const string Payload = """
            {
              "id": "evt_test",
              "object": "event",
              "created": 1710000000,
              "livemode": false,
              "type": "customer.subscription.updated",
              "data": {
                "object": {
                  "id": "sub_test",
                  "object": "subscription"
                }
              }
            }
            """;

        var snapshot = await client.ValidateWebhookEventAsync(Payload, "t=1,v1=test", CancellationToken.None);

        Assert.Equal(PaymentProviderNames.Stripe, snapshot.Provider);
        Assert.Equal("evt_test", snapshot.ProviderEventId);
        Assert.Equal("sub_test", snapshot.ObjectId);
        Assert.Equal("subscription", snapshot.ObjectType);
    }

    [Fact]
    public async Task BillingClientAcceptsRealStripeSignedWebhookPayload()
    {
        const string WebhookSecret = "whsec_test_secret";
        const string Payload = """
            {
              "id": "evt_signed",
              "object": "event",
              "created": 1710000000,
              "livemode": false,
              "type": "customer.subscription.updated",
              "data": {
                "object": {
                  "id": "sub_signed",
                  "object": "subscription",
                  "metadata": {
                    "project_id": "project-signed"
                  }
                }
              }
            }
            """;
        var client = new StripeBillingClient(
            new StripeBillingSdk(new FixedStripeApiKeyProvider("sk_test")),
            new FixedStripeWebhookSecretProvider(WebhookSecret));

        var snapshot = await client.ValidateWebhookEventAsync(
            Payload,
            CreateStripeSignatureHeader(Payload, WebhookSecret),
            CancellationToken.None);

        Assert.Equal("evt_signed", snapshot.EventId);
        Assert.Equal("customer.subscription.updated", snapshot.EventType);
        Assert.Equal("sub_signed", snapshot.ObjectId);
        Assert.Equal("project-signed", snapshot.Metadata[StripeBillingClient.ProjectMetadataKey]);
    }

    [Fact]
    public async Task BillingClientRejectsRealStripeWebhookPayloadWithInvalidSignature()
    {
        const string Payload = """
            {
              "id": "evt_invalid_signature",
              "object": "event",
              "created": 1710000000,
              "livemode": false,
              "type": "customer.subscription.updated",
              "data": {
                "object": {
                  "id": "sub_invalid_signature",
                  "object": "subscription"
                }
              }
            }
            """;
        var client = new StripeBillingClient(
            new StripeBillingSdk(new FixedStripeApiKeyProvider("sk_test")),
            new FixedStripeWebhookSecretProvider("whsec_expected"));

        await Assert.ThrowsAsync<StripeException>(async () =>
            await client.ValidateWebhookEventAsync(
                Payload,
                CreateStripeSignatureHeader(Payload, "whsec_wrong"),
                CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task BillingClientReconcilesInvoiceWithSubscriptionMetadataFallback()
    {
        var paidAt = DateTime.UtcNow;
        var sdk = new CapturingStripeBillingSdk
        {
            Invoice = new Invoice
            {
                Id = "in_test",
                CustomerId = "cus_test",
                Status = "paid",
                Currency = "usd",
                AmountDue = 1200,
                AmountPaid = 1200,
                AmountRemaining = 0,
                PeriodStart = new DateTime(2026, 06, 01, 0, 0, 0, DateTimeKind.Utc),
                PeriodEnd = new DateTime(2026, 07, 01, 0, 0, 0, DateTimeKind.Utc),
                HostedInvoiceUrl = "https://invoice.stripe.test/hosted",
                InvoicePdf = "https://invoice.stripe.test/pdf",
                StatusTransitions = new InvoiceStatusTransitions
                {
                    PaidAt = paidAt,
                },
                Metadata = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["invoice_source"] = "stripe",
                },
                Parent = new InvoiceParent
                {
                    SubscriptionDetails = new InvoiceParentSubscriptionDetails
                    {
                        SubscriptionId = "sub_test",
                        Metadata = new Dictionary<string, string>(StringComparer.Ordinal)
                        {
                            [StripeBillingClient.TenantMetadataKey] = "01ARZ3NDEKTSV4RRFFQ69G5FAV",
                            [StripeBillingClient.ProjectMetadataKey] = "project-1",
                            [StripeBillingClient.TierMetadataKey] = "Starter",
                        },
                    },
                },
            },
        };
        var client = new StripeBillingClient(sdk);

        var snapshot = await client.ReconcileInvoiceAsync("in_test");

        Assert.Equal("in_test", snapshot.InvoiceId);
        Assert.Equal("sub_test", snapshot.StripeSubscriptionId);
        Assert.Equal("paid", snapshot.Status);
        Assert.Equal(1200, snapshot.AmountPaid);
        Assert.Equal(paidAt, snapshot.PaidAt);
        Assert.Equal("01ARZ3NDEKTSV4RRFFQ69G5FAV", snapshot.TenantId);
        Assert.Equal("project-1", snapshot.ProjectId);
        Assert.Equal("stripe", snapshot.Metadata["invoice_source"]);
    }

    [Theory]
    [InlineData("tok_test_123")]
    [InlineData("billing@example.com")]
    public async Task BillingClientRejectsSensitiveInvoiceLookupId(string invoiceId)
    {
        IPaymentInvoiceReconciliationClient client = new StripeBillingClient(new CapturingStripeBillingSdk());

        var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
            await client.ReconcileInvoiceAsync(invoiceId, CancellationToken.None));

        Assert.Equal("invoiceId", exception.ParamName);
        Assert.Contains("sensitive", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BillingClientStripsSensitiveInboundInvoiceMetadata()
    {
        var sdk = new CapturingStripeBillingSdk
        {
            Invoice = new Invoice
            {
                Id = "in_test",
                CustomerId = "cus_test",
                Status = "paid",
                Currency = "usd",
                AmountDue = 1200,
                AmountPaid = 1200,
                AmountRemaining = 0,
                Metadata = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["invoice_source"] = "sk_test_123",
                    ["card_token"] = "tok_test",
                    ["billing_email"] = "billing@example.com",
                },
                Parent = new InvoiceParent
                {
                    SubscriptionDetails = new InvoiceParentSubscriptionDetails
                    {
                        SubscriptionId = "sub_test",
                        Metadata = new Dictionary<string, string>(StringComparer.Ordinal)
                        {
                            [StripeBillingClient.TenantMetadataKey] = "01ARZ3NDEKTSV4RRFFQ69G5FAV",
                            [StripeBillingClient.ProjectMetadataKey] = "project-1",
                            ["operator_secret"] = "secret",
                        },
                    },
                },
            },
        };
        IPaymentInvoiceReconciliationClient client = new StripeBillingClient(sdk);

        var snapshot = await client.ReconcileInvoiceAsync("in_test", CancellationToken.None);

        Assert.Equal("project-1", snapshot.ProjectId);
        Assert.False(snapshot.Metadata.ContainsKey("invoice_source"));
        Assert.False(snapshot.Metadata.ContainsKey("card_token"));
        Assert.False(snapshot.Metadata.ContainsKey("billing_email"));
        Assert.False(snapshot.Metadata.ContainsKey("operator_secret"));
    }

    [Fact]
    public async Task BillingClientReconcilesInvoiceThroughProviderNeutralContract()
    {
        var paidAt = DateTime.UtcNow;
        var sdk = new CapturingStripeBillingSdk
        {
            Invoice = new Invoice
            {
                Id = "in_test",
                CustomerId = "cus_test",
                Status = "open",
                Currency = "usd",
                AmountDue = 2400,
                AmountPaid = 1200,
                AmountRemaining = 1200,
                PeriodStart = new DateTime(2026, 06, 01, 0, 0, 0, DateTimeKind.Utc),
                PeriodEnd = new DateTime(2026, 07, 01, 0, 0, 0, DateTimeKind.Utc),
                HostedInvoiceUrl = "https://invoice.stripe.test/hosted",
                InvoicePdf = "https://invoice.stripe.test/pdf",
                StatusTransitions = new InvoiceStatusTransitions
                {
                    PaidAt = paidAt,
                },
                Metadata = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    [StripeBillingClient.TenantMetadataKey] = "01ARZ3NDEKTSV4RRFFQ69G5FAV",
                    [StripeBillingClient.ProjectMetadataKey] = "project-1",
                    [StripeBillingClient.TierMetadataKey] = "Starter",
                },
                Parent = new InvoiceParent
                {
                    SubscriptionDetails = new InvoiceParentSubscriptionDetails
                    {
                        SubscriptionId = "sub_test",
                    },
                },
            },
        };
        IPaymentInvoiceReconciliationClient client = new StripeBillingClient(sdk);

        var snapshot = await client.ReconcileInvoiceAsync("in_test", CancellationToken.None);

        Assert.Equal("in_test", sdk.LastInvoiceId);
        Assert.Equal(PaymentProviderNames.Stripe, snapshot.Provider);
        Assert.Equal("in_test", snapshot.ProviderInvoiceId);
        Assert.Equal("cus_test", snapshot.ProviderCustomerId);
        Assert.Equal("sub_test", snapshot.ProviderSubscriptionId);
        Assert.Equal("open", snapshot.Status);
        Assert.Equal(1200, snapshot.AmountRemaining);
        Assert.Equal("project-1", snapshot.ProjectId);
        Assert.Equal("Starter", snapshot.TierName);
    }

    [Fact]
    public async Task NoopMeteredBillingClientAcceptsValidMeterEvent()
    {
        var client = new NoopStripeMeteredBillingClient();
        var meterEvent = new StripeMeterEvent(
            "payments.submission.accepted.email",
            "01ARZ3NDEKTSV4RRFFQ69G5FAV",
            1,
            DateTimeOffset.UtcNow,
            "bill-event-1",
            "corr-1",
            new Dictionary<string, string>(StringComparer.Ordinal));

        await client.RecordMeterEventAsync(meterEvent, CancellationToken.None);
    }

    [Fact]
    public async Task NoopMeteredBillingClientObservesCancellation()
    {
        var client = new NoopStripeMeteredBillingClient();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var meterEvent = new StripeMeterEvent(
            "payments.submission.accepted.email",
            "01ARZ3NDEKTSV4RRFFQ69G5FAV",
            1,
            DateTimeOffset.UtcNow,
            "bill-event-1",
            "corr-1",
            new Dictionary<string, string>(StringComparer.Ordinal));

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await client.RecordMeterEventAsync(meterEvent, cancellation.Token));
    }

    private static string CreateStripeSignatureHeader(string payload, string webhookSecret)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var signedPayload = $"{timestamp}.{payload}";
        using var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(webhookSecret));
        var signature = Convert.ToHexString(hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(signedPayload))).ToLowerInvariant();
        return $"t={timestamp},v1={signature}";
    }

    private sealed class CapturingStripeMeterEventBuffer : IStripeMeterEventBuffer
    {
        public StripeMeterEvent? LastEvent { get; private set; }

        public ValueTask EnqueueAsync(StripeMeterEvent meterEvent, CancellationToken cancellationToken)
        {
            LastEvent = meterEvent;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class CapturingStripeMeteredBillingClient : IStripeMeteredBillingClient
    {
        public StripeMeterEvent? LastEvent { get; private set; }

        public ValueTask RecordMeterEventAsync(StripeMeterEvent meterEvent, CancellationToken cancellationToken)
        {
            LastEvent = meterEvent;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class FixedStripeApiKeyProvider(string apiKey) : IStripeApiKeyProvider
    {
        public ValueTask<string> GetApiKeyAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(apiKey);
        }
    }

    private sealed class FixedStripeWebhookSecretProvider(string webhookSecret) : IStripeWebhookSecretProvider
    {
        public ValueTask<string> GetWebhookSecretAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(webhookSecret);
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class CapturingStripeBillingSdk : IStripeBillingSdk
    {
        public MeterEventCreateOptions? LastMeterEventOptions { get; private set; }

        public StripeCheckout.SessionCreateOptions? LastCheckoutSessionOptions { get; private set; }

        public SubscriptionCancelOptions? LastSubscriptionCancelOptions { get; private set; }

        public string? LastSubscriptionId { get; private set; }

        public string? LastInvoiceId { get; private set; }

        public string? LastIdempotencyKey { get; private set; }

        public string? LastWebhookPayload { get; private set; }

        public string? LastWebhookSignature { get; private set; }

        public string? LastWebhookSecret { get; private set; }

        public StripeCheckout.Session CheckoutSession { get; init; } = new()
        {
            Id = "cs_default",
        };

        public Subscription Subscription { get; init; } = new()
        {
            Id = "sub_default",
            Metadata = new Dictionary<string, string>(StringComparer.Ordinal),
        };

        public Invoice Invoice { get; init; } = new()
        {
            Id = "in_default",
            Metadata = new Dictionary<string, string>(StringComparer.Ordinal),
        };

        public Task<MeterEvent> CreateMeterEventAsync(
            MeterEventCreateOptions options,
            string? idempotencyKey,
            CancellationToken cancellationToken)
        {
            LastMeterEventOptions = options;
            LastIdempotencyKey = idempotencyKey;
            return Task.FromResult(new MeterEvent());
        }

        public Task<StripeCheckout.Session> CreateCheckoutSessionAsync(
            StripeCheckout.SessionCreateOptions options,
            string? idempotencyKey,
            CancellationToken cancellationToken)
        {
            LastCheckoutSessionOptions = options;
            LastIdempotencyKey = idempotencyKey;
            return Task.FromResult(CheckoutSession);
        }

        public Task<Subscription> GetSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken)
        {
            LastSubscriptionId = subscriptionId;
            return Task.FromResult(Subscription);
        }

        public Task<Subscription> CancelSubscriptionAsync(
            string subscriptionId,
            SubscriptionCancelOptions options,
            string? idempotencyKey,
            CancellationToken cancellationToken)
        {
            LastSubscriptionId = subscriptionId;
            LastSubscriptionCancelOptions = options;
            LastIdempotencyKey = idempotencyKey;
            return Task.FromResult(Subscription);
        }

        public Event ConstructEvent(string payload, string signatureHeader, string webhookSecret)
        {
            LastWebhookPayload = payload;
            LastWebhookSignature = signatureHeader;
            LastWebhookSecret = webhookSecret;
            return EventUtility.ParseEvent(payload, throwOnApiVersionMismatch: false);
        }

        public Task<Invoice> GetInvoiceAsync(string invoiceId, CancellationToken cancellationToken)
        {
            LastInvoiceId = invoiceId;
            return Task.FromResult(Invoice);
        }
    }
}
