namespace HoneyDrunk.Payments.ProviderTesting;

public abstract class PaymentProviderContractTests
{
    public const string BillingEventIdAttributeKey = "billing_event_id";

    [Fact]
    public async Task ProviderCreatesCheckoutSessionWithProviderNeutralSnapshot()
    {
        var fixture = CreateFixture();
        var session = await fixture.SubscriptionLifecycleClient.CreateCheckoutSessionAsync(fixture.CreateCheckoutRequest(), CancellationToken.None);

        Assert.Equal(fixture.ProviderName, session.Provider);
        Assert.False(string.IsNullOrWhiteSpace(session.SessionId));
        Assert.Equal(fixture.Expectations.TenantId, session.TenantId);
        Assert.Equal(fixture.Expectations.ProjectId, session.ProjectId);
        Assert.Equal(fixture.Expectations.TierName, session.TierName);
        Assert.Equal(fixture.Expectations.ProviderCustomerId, session.ProviderCustomerId);
        Assert.Equal(fixture.Expectations.ProviderSubscriptionId, session.ProviderSubscriptionId);
    }

    [Fact]
    public async Task ProviderReadsSubscriptionWithProviderNeutralSnapshot()
    {
        var fixture = CreateFixture();
        var subscription = await fixture.SubscriptionLifecycleClient.GetSubscriptionAsync(
            fixture.Expectations.ProviderSubscriptionId,
            CancellationToken.None);

        Assert.Equal(fixture.ProviderName, subscription.Provider);
        Assert.Equal(fixture.Expectations.ProviderSubscriptionId, subscription.ProviderSubscriptionId);
        Assert.Equal(fixture.Expectations.ProviderCustomerId, subscription.ProviderCustomerId);
        Assert.Equal(fixture.Expectations.TenantId, subscription.TenantId);
        Assert.Equal(fixture.Expectations.ProjectId, subscription.ProjectId);
        Assert.Equal(fixture.Expectations.TierName, subscription.TierName);
        Assert.False(string.IsNullOrWhiteSpace(subscription.Status));
    }

    [Fact]
    public async Task ProviderCancelsSubscriptionWithProviderNeutralSnapshot()
    {
        var fixture = CreateFixture();
        var subscription = await fixture.SubscriptionLifecycleClient.CancelSubscriptionAsync(
            fixture.CreateCancellationRequest(),
            CancellationToken.None);

        Assert.Equal(fixture.ProviderName, subscription.Provider);
        Assert.Equal(fixture.Expectations.ProviderSubscriptionId, subscription.ProviderSubscriptionId);
        Assert.False(string.IsNullOrWhiteSpace(subscription.Status));
    }

    [Fact]
    public async Task ProviderValidatesWebhookWithProviderNeutralSnapshot()
    {
        var fixture = CreateFixture();
        var webhookEvent = await fixture.WebhookEventValidator.ValidateWebhookEventAsync(
            fixture.Expectations.WebhookPayload,
            fixture.Expectations.WebhookSignatureHeader,
            CancellationToken.None);

        Assert.Equal(fixture.ProviderName, webhookEvent.Provider);
        Assert.Equal(fixture.Expectations.WebhookProviderEventId, webhookEvent.ProviderEventId);
        Assert.Equal(fixture.Expectations.WebhookEventType, webhookEvent.EventType);
        Assert.False(string.IsNullOrWhiteSpace(webhookEvent.ObjectId));
        Assert.False(string.IsNullOrWhiteSpace(webhookEvent.ObjectType));
    }

    [Fact]
    public async Task ProviderReconcilesInvoiceWithProviderNeutralSnapshot()
    {
        var fixture = CreateFixture();
        var invoice = await fixture.InvoiceReconciliationClient.ReconcileInvoiceAsync(
            fixture.Expectations.ProviderInvoiceId,
            CancellationToken.None);

        Assert.Equal(fixture.ProviderName, invoice.Provider);
        Assert.Equal(fixture.Expectations.ProviderInvoiceId, invoice.ProviderInvoiceId);
        Assert.Equal(fixture.Expectations.ProviderCustomerId, invoice.ProviderCustomerId);
        Assert.Equal(fixture.Expectations.ProviderSubscriptionId, invoice.ProviderSubscriptionId);
        Assert.Equal(fixture.Expectations.TenantId, invoice.TenantId);
        Assert.Equal(fixture.Expectations.ProjectId, invoice.ProjectId);
        Assert.Equal(fixture.Expectations.TierName, invoice.TierName);
        Assert.True(invoice.AmountDue >= 0);
        Assert.True(invoice.AmountPaid >= 0);
        Assert.True(invoice.AmountRemaining >= 0);
    }

    [Fact]
    public async Task ProviderEmitsMeterEventWhenPerEventIdempotencyIsPresent()
    {
        var fixture = CreateFixture();
        var billingEvent = fixture.CreateBillingEvent(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [BillingEventIdAttributeKey] = "contract-billing-event-1",
        });

        await fixture.BillingEventEmitter.EmitAsync(billingEvent, CancellationToken.None);
    }

    [Fact]
    public async Task ProviderRejectsMeterEventWithoutPerEventIdempotency()
    {
        var fixture = CreateFixture();
        var billingEvent = fixture.CreateBillingEvent(new Dictionary<string, string>(StringComparer.Ordinal));

        var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
            await fixture.BillingEventEmitter.EmitAsync(billingEvent, CancellationToken.None));

        Assert.Contains(BillingEventIdAttributeKey, exception.Message, StringComparison.Ordinal);
    }

    protected abstract PaymentProviderContractFixture CreateFixture();
}
