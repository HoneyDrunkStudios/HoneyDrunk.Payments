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
    public async Task EmitAsyncRecordsKernelBillingEventAsStripeMeterEvent()
    {
        var client = new CapturingStripeMeteredBillingClient();
        var adapter = new StripeBillingEventEmitter(client);
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
                ["project_id"] = "project-1",
            });

        await adapter.EmitAsync(billingEvent, CancellationToken.None);

        Assert.NotNull(client.LastEvent);
        Assert.Equal("payments.submission.accepted.sms", client.LastEvent.EventName);
        Assert.Equal(tenantId.ToString(), client.LastEvent.CustomerKey);
        Assert.Equal(2, client.LastEvent.Units);
        Assert.Equal(occurredAtUtc, client.LastEvent.OccurredAtUtc);
        Assert.Equal("corr-1", client.LastEvent.CorrelationId);
        Assert.Equal("project-1", client.LastEvent.Metadata["project_id"]);
    }

    [Fact]
    public async Task EmitAsyncSkipsInternalTenantEvents()
    {
        var client = new CapturingStripeMeteredBillingClient();
        var adapter = new StripeBillingEventEmitter(client);
        var billingEvent = new BillingEvent(
            TenantId.Internal,
            "payments.submission.accepted",
            "email",
            Units: 1,
            DateTimeOffset.UtcNow,
            "corr-1",
            new Dictionary<string, string>(StringComparer.Ordinal));

        await adapter.EmitAsync(billingEvent, CancellationToken.None);

        Assert.Null(client.LastEvent);
    }

    [Fact]
    public async Task EmitAsyncRejectsNonPositiveUnits()
    {
        var adapter = new StripeBillingEventEmitter();
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
    public async Task BillingClientRecordsMeterEventsThroughStripeSdkPayload()
    {
        var sdk = new CapturingStripeBillingSdk();
        var client = new StripeBillingClient(sdk);
        var occurredAtUtc = new DateTimeOffset(2026, 6, 14, 12, 30, 0, TimeSpan.Zero);
        var meterEvent = new StripeMeterEvent(
            "payments.submission.accepted.email",
            "01ARZ3NDEKTSV4RRFFQ69G5FAV",
            3,
            occurredAtUtc,
            "corr-1",
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["project_id"] = "project-1",
            });

        await client.RecordMeterEventAsync(meterEvent, CancellationToken.None);

        Assert.NotNull(sdk.LastMeterEventOptions);
        Assert.Equal("payments.submission.accepted.email", sdk.LastMeterEventOptions.EventName);
        Assert.Equal("corr-1", sdk.LastMeterEventOptions.Identifier);
        Assert.Equal(occurredAtUtc.UtcDateTime, sdk.LastMeterEventOptions.Timestamp);
        Assert.Equal("corr-1", sdk.LastIdempotencyKey);
        Assert.Equal("01ARZ3NDEKTSV4RRFFQ69G5FAV", sdk.LastMeterEventOptions.Payload[StripeBillingClient.MeterCustomerPayloadKey]);
        Assert.Equal("3", sdk.LastMeterEventOptions.Payload[StripeBillingClient.MeterValuePayloadKey]);
        Assert.Equal("project-1", sdk.LastMeterEventOptions.Payload["project_id"]);
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
        Assert.Equal("billing@example.com", sdk.LastCheckoutSessionOptions.CustomerEmail);
        Assert.Equal("price_starter", Assert.Single(sdk.LastCheckoutSessionOptions.LineItems).Price);
        Assert.Equal("checkout-1", sdk.LastIdempotencyKey);
        Assert.Equal("01ARZ3NDEKTSV4RRFFQ69G5FAV", sdk.LastCheckoutSessionOptions.Metadata[StripeBillingClient.TenantMetadataKey]);
        Assert.Equal("project-1", sdk.LastCheckoutSessionOptions.SubscriptionData.Metadata[StripeBillingClient.ProjectMetadataKey]);
        Assert.Equal("Starter", sdk.LastCheckoutSessionOptions.SubscriptionData.Metadata[StripeBillingClient.TierMetadataKey]);
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
    public void BillingClientNormalizesWebhookEventMetadata()
    {
        var sdk = new CapturingStripeBillingSdk();
        var client = new StripeBillingClient(sdk);
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

        var snapshot = client.ValidateWebhookEvent(Payload, "t=1,v1=test", "whsec_test");

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
    public void BillingClientSupportsProviderNeutralWebhookContract()
    {
        var sdk = new CapturingStripeBillingSdk();
        IPaymentWebhookEventValidator client = new StripeBillingClient(sdk);
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

        var snapshot = client.ValidateWebhookEvent(Payload, "t=1,v1=test", "whsec_test");

        Assert.Equal(PaymentProviderNames.Stripe, snapshot.Provider);
        Assert.Equal("evt_test", snapshot.ProviderEventId);
        Assert.Equal("sub_test", snapshot.ObjectId);
        Assert.Equal("subscription", snapshot.ObjectType);
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
            "corr-1",
            new Dictionary<string, string>(StringComparer.Ordinal));

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await client.RecordMeterEventAsync(meterEvent, cancellation.Token));
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
