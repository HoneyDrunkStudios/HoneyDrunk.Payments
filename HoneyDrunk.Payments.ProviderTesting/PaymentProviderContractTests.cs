namespace HoneyDrunk.Payments.ProviderTesting;

/// <summary>
/// Reusable provider-neutral payment contract tests for provider test projects.
/// </summary>
public abstract class PaymentProviderContractTests
{
    /// <summary>
    /// Attribute key that carries the per-event billing idempotency value.
    /// </summary>
    public const string BillingEventIdAttributeKey = "billing_event_id";

    /// <summary>
    /// Verifies that the provider creates checkout sessions with normalized payment snapshots.
    /// </summary>
    /// <returns>A task that completes when the assertion run finishes.</returns>
    protected async Task AssertProviderCreatesCheckoutSessionWithProviderNeutralSnapshotAsync()
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

    /// <summary>
    /// Verifies that the provider reads subscriptions with normalized subscription snapshots.
    /// </summary>
    /// <returns>A task that completes when the assertion run finishes.</returns>
    protected async Task AssertProviderReadsSubscriptionWithProviderNeutralSnapshotAsync()
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

    /// <summary>
    /// Verifies that the provider cancels subscriptions with normalized subscription snapshots.
    /// </summary>
    /// <returns>A task that completes when the assertion run finishes.</returns>
    protected async Task AssertProviderCancelsSubscriptionWithProviderNeutralSnapshotAsync()
    {
        var fixture = CreateFixture();
        var subscription = await fixture.SubscriptionLifecycleClient.CancelSubscriptionAsync(
            fixture.CreateCancellationRequest(),
            CancellationToken.None);

        Assert.Equal(fixture.ProviderName, subscription.Provider);
        Assert.Equal(fixture.Expectations.ProviderSubscriptionId, subscription.ProviderSubscriptionId);
        Assert.False(string.IsNullOrWhiteSpace(subscription.Status));
    }

    /// <summary>
    /// Verifies that the provider validates signed webhook events with normalized event snapshots.
    /// </summary>
    /// <returns>A task that completes when the assertion run finishes.</returns>
    protected async Task AssertProviderValidatesWebhookWithProviderNeutralSnapshotAsync()
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

    /// <summary>
    /// Verifies that the provider reconciles invoices with normalized invoice snapshots.
    /// </summary>
    /// <returns>A task that completes when the assertion run finishes.</returns>
    protected async Task AssertProviderReconcilesInvoiceWithProviderNeutralSnapshotAsync()
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

    /// <summary>
    /// Verifies that the provider accepts meter events that include per-event idempotency.
    /// </summary>
    /// <returns>A task that completes when the assertion run finishes.</returns>
    protected async Task AssertProviderEmitsMeterEventWhenPerEventIdempotencyIsPresentAsync()
    {
        var fixture = CreateFixture();
        var billingEvent = fixture.CreateBillingEvent(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [BillingEventIdAttributeKey] = "contract-billing-event-1",
        });

        await fixture.BillingEventEmitter.EmitAsync(billingEvent, CancellationToken.None);
    }

    /// <summary>
    /// Verifies that the provider rejects meter events that omit per-event idempotency.
    /// </summary>
    /// <returns>A task that completes when the assertion run finishes.</returns>
    protected async Task AssertProviderRejectsMeterEventWithoutPerEventIdempotencyAsync()
    {
        var fixture = CreateFixture();
        var billingEvent = fixture.CreateBillingEvent(new Dictionary<string, string>(StringComparer.Ordinal));

        var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
            await fixture.BillingEventEmitter.EmitAsync(billingEvent, CancellationToken.None));

        Assert.Contains(BillingEventIdAttributeKey, exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// Creates the provider fixture used by the reusable contract tests.
    /// </summary>
    /// <returns>A provider fixture with clients and expectations configured for the provider under test.</returns>
    protected abstract PaymentProviderContractFixture CreateFixture();
}
