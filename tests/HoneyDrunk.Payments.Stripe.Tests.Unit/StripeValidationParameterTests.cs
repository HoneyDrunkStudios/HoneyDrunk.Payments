using HoneyDrunk.Payments.Stripe;

namespace HoneyDrunk.Payments.Stripe.Tests.Unit;

public sealed class StripeValidationParameterTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void NullMetadataValuesPreserveTheCallerParameter(bool providerReference)
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
        {
            if (providerReference)
            {
                StripeMetadataPolicy.ValidateProviderReferenceValue(null!, "externalReference");
            }
            else
            {
                StripeMetadataPolicy.ValidateOutboundMetadataValue(null!, "externalReference");
            }
        });
        Assert.Equal("externalReference", exception.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void InvalidMeterEventNamesPreserveTheCallerParameter(string? value)
    {
        var exception = Assert.ThrowsAny<ArgumentException>(() => StripeMeterEventNamePolicy.ValidateEventName(value!, "meterEvent"));
        Assert.Equal("meterEvent", exception.ParamName);
        Assert.Equal(value is null, exception is ArgumentNullException);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void EmptyMetadataKeysPreserveTheCallerParameter(string key)
    {
        var exception = Assert.Throws<ArgumentException>(() => StripeMetadataPolicy.CopyOutboundMetadata(new Dictionary<string, string> { [key] = "value" }, "metadata"));
        Assert.Equal("metadata", exception.ParamName);
    }
}
