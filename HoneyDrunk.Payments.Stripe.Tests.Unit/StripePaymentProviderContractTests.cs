using HoneyDrunk.Payments.Abstractions;
using HoneyDrunk.Payments.Tests.Unit;
using Stripe;
using Stripe.Billing;
using KernelBillingEventEmitter = HoneyDrunk.Kernel.Abstractions.Tenancy.IBillingEventEmitter;
using StripeCheckout = Stripe.Checkout;

namespace HoneyDrunk.Payments.Stripe.Tests.Unit;

public sealed class StripePaymentProviderContractTests : PaymentProviderContractTests
{
    /// <summary>
    /// Verifies that Stripe creates checkout sessions with normalized payment snapshots.
    /// </summary>
    /// <returns>A task that completes when the assertion run finishes.</returns>
    [Fact]
    public Task ProviderCreatesCheckoutSessionWithProviderNeutralSnapshot() =>
        AssertProviderCreatesCheckoutSessionWithProviderNeutralSnapshotAsync();

    /// <summary>
    /// Verifies that Stripe reads subscriptions with normalized subscription snapshots.
    /// </summary>
    /// <returns>A task that completes when the assertion run finishes.</returns>
    [Fact]
    public Task ProviderReadsSubscriptionWithProviderNeutralSnapshot() =>
        AssertProviderReadsSubscriptionWithProviderNeutralSnapshotAsync();

    /// <summary>
    /// Verifies that Stripe cancels subscriptions with normalized subscription snapshots.
    /// </summary>
    /// <returns>A task that completes when the assertion run finishes.</returns>
    [Fact]
    public Task ProviderCancelsSubscriptionWithProviderNeutralSnapshot() =>
        AssertProviderCancelsSubscriptionWithProviderNeutralSnapshotAsync();

    /// <summary>
    /// Verifies that Stripe validates signed webhook events with normalized event snapshots.
    /// </summary>
    /// <returns>A task that completes when the assertion run finishes.</returns>
    [Fact]
    public Task ProviderValidatesWebhookWithProviderNeutralSnapshot() =>
        AssertProviderValidatesWebhookWithProviderNeutralSnapshotAsync();

    /// <summary>
    /// Verifies that Stripe rejects webhook payloads with invalid signatures.
    /// </summary>
    /// <returns>A task that completes when the assertion run finishes.</returns>
    [Fact]
    public Task ProviderRejectsWebhookWithInvalidSignature() =>
        AssertProviderRejectsWebhookWithInvalidSignatureAsync();

    /// <summary>
    /// Verifies that Stripe reconciles invoices with normalized invoice snapshots.
    /// </summary>
    /// <returns>A task that completes when the assertion run finishes.</returns>
    [Fact]
    public Task ProviderReconcilesInvoiceWithProviderNeutralSnapshot() =>
        AssertProviderReconcilesInvoiceWithProviderNeutralSnapshotAsync();

    /// <summary>
    /// Verifies that Stripe accepts meter events that include per-event idempotency.
    /// </summary>
    /// <returns>A task that completes when the assertion run finishes.</returns>
    [Fact]
    public Task ProviderEmitsMeterEventWhenPerEventIdempotencyIsPresent() =>
        AssertProviderEmitsMeterEventWhenPerEventIdempotencyIsPresentAsync();

    /// <summary>
    /// Verifies that Stripe rejects meter events that omit per-event idempotency.
    /// </summary>
    /// <returns>A task that completes when the assertion run finishes.</returns>
    [Fact]
    public Task ProviderRejectsMeterEventWithoutPerEventIdempotency() =>
        AssertProviderRejectsMeterEventWithoutPerEventIdempotencyAsync();

    /// <summary>
    /// Verifies that Stripe rejects meter events that omit provider customer mapping.
    /// </summary>
    /// <returns>A task that completes when the assertion run finishes.</returns>
    [Fact]
    public Task ProviderRejectsMeterEventWithoutProviderCustomerId() =>
        AssertProviderRejectsMeterEventWithoutProviderCustomerIdAsync();

    protected override PaymentProviderContractFixture CreateFixture() => new StripeContractFixture();

    private sealed class StripeContractFixture : PaymentProviderContractFixture
    {
        private const string Payload = """
            {
              "id": "evt_contract",
              "object": "event",
              "created": 1710000000,
              "livemode": false,
              "type": "customer.subscription.updated",
              "data": {
                "object": {
                  "id": "sub_contract",
                  "object": "subscription",
                  "metadata": {
                    "payments_tenant_id": "01ARZ3NDEKTSV4RRFFQ69G5FAV",
                    "project_id": "project-contract",
                    "tier_name": "Starter"
                  }
                }
              }
            }
            """;

        private readonly StripeBillingClient client;

        public StripeContractFixture()
            : base(new PaymentProviderContractExpectations(
                "01ARZ3NDEKTSV4RRFFQ69G5FAV",
                "project-contract",
                "Starter",
                "price_contract",
                "billing@example.com",
                "cus_contract",
                "sub_contract",
                "in_contract",
                "evt_contract",
                "customer.subscription.updated",
                Payload,
                "t=1,v1=valid",
                "t=1,v1=invalid"))
        {
            client = new StripeBillingClient(
                new ContractStripeBillingSdk(),
                new FixedStripeWebhookSecretProvider("whsec_contract"));
        }

        public override string ProviderName => PaymentProviderNames.Stripe;

        public override IPaymentSubscriptionLifecycleClient SubscriptionLifecycleClient => client;

        public override IPaymentWebhookEventValidator WebhookEventValidator => client;

        public override IPaymentInvoiceReconciliationClient InvoiceReconciliationClient => client;

        public override KernelBillingEventEmitter BillingEventEmitter { get; } =
            new StripeBillingEventEmitter(new ContractStripeMeterEventBuffer());
    }

    private sealed class FixedStripeWebhookSecretProvider(string webhookSecret) : IStripeWebhookSecretProvider
    {
        public ValueTask<string> GetWebhookSecretAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(webhookSecret);
        }
    }

    private sealed class ContractStripeMeterEventBuffer : IStripeMeterEventBuffer
    {
        public ValueTask EnqueueAsync(StripeMeterEvent meterEvent, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(meterEvent);
            ArgumentException.ThrowIfNullOrWhiteSpace(meterEvent.IdempotencyKey);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class ContractStripeBillingSdk : IStripeBillingSdk
    {
        public Task<MeterEvent> CreateMeterEventAsync(
            MeterEventCreateOptions options,
            string? idempotencyKey,
            CancellationToken cancellationToken) =>
            Task.FromResult(new MeterEvent());

        public Task<StripeCheckout.Session> CreateCheckoutSessionAsync(
            StripeCheckout.SessionCreateOptions options,
            string? idempotencyKey,
            CancellationToken cancellationToken) =>
            Task.FromResult(new StripeCheckout.Session
            {
                Id = "cs_contract",
                Url = "https://checkout.stripe.test/contract",
                CustomerId = "cus_contract",
                SubscriptionId = "sub_contract",
            });

        public Task<Subscription> GetSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken) =>
            Task.FromResult(new Subscription
            {
                Id = subscriptionId,
                CustomerId = "cus_contract",
                Status = "active",
                Created = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                LatestInvoiceId = "in_contract",
                Metadata = PaymentsMetadata(),
            });

        public Task<Subscription> CancelSubscriptionAsync(
            string subscriptionId,
            SubscriptionCancelOptions options,
            string? idempotencyKey,
            CancellationToken cancellationToken) =>
            Task.FromResult(new Subscription
            {
                Id = subscriptionId,
                CustomerId = "cus_contract",
                Status = "canceled",
                CanceledAt = new DateTime(2026, 6, 14, 0, 0, 0, DateTimeKind.Utc),
                Created = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                Metadata = PaymentsMetadata(),
            });

        public Event ConstructEvent(string payload, string signatureHeader, string webhookSecret)
        {
            if (!StringComparer.Ordinal.Equals(signatureHeader, "t=1,v1=valid"))
            {
                throw new StripeException("Invalid webhook signature.");
            }

            return EventUtility.ParseEvent(payload, throwOnApiVersionMismatch: false);
        }

        public Task<Invoice> GetInvoiceAsync(string invoiceId, CancellationToken cancellationToken) =>
            Task.FromResult(new Invoice
            {
                Id = invoiceId,
                CustomerId = "cus_contract",
                Status = "paid",
                Currency = "usd",
                AmountDue = 1200,
                AmountPaid = 1200,
                AmountRemaining = 0,
                PeriodStart = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                PeriodEnd = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc),
                Metadata = PaymentsMetadata(),
                Parent = new InvoiceParent
                {
                    SubscriptionDetails = new InvoiceParentSubscriptionDetails
                    {
                        SubscriptionId = "sub_contract",
                    },
                },
            });

        private static Dictionary<string, string> PaymentsMetadata() =>
            new(StringComparer.Ordinal)
            {
                [StripeBillingClient.TenantMetadataKey] = "01ARZ3NDEKTSV4RRFFQ69G5FAV",
                [StripeBillingClient.ProjectMetadataKey] = "project-contract",
                [StripeBillingClient.TierMetadataKey] = "Starter",
            };
    }
}
