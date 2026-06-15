namespace HoneyDrunk.Payments.Stripe;

internal static class StripeMetadataPolicy
{
    internal const int MaxCustomMetadataEntries = 40;
    internal const int MaxMetadataKeyLength = 40;
    internal const int MaxMetadataValueLength = 500;

    private static readonly string[] SensitiveKeyFragments =
    [
        "api_key",
        "apikey",
        "address",
        "authorization",
        "card",
        "credential",
        "email",
        "password",
        "phone",
        "secret",
        "signature",
        "ssn",
        "token",
    ];

    public static Dictionary<string, string> CopyOutboundMetadata(
        IReadOnlyDictionary<string, string>? metadata,
        string parameterName)
    {
        if (metadata is null)
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        if (metadata.Count > MaxCustomMetadataEntries)
        {
            throw new ArgumentException(
                $"Stripe metadata cannot contain more than {MaxCustomMetadataEntries} custom entries.",
                parameterName);
        }

        var copy = new Dictionary<string, string>(metadata.Count, StringComparer.Ordinal);
        foreach (var (key, value) in metadata)
        {
            ValidateKey(key, parameterName);
            ValidateValue(value, parameterName);
            copy[key] = value;
        }

        return copy;
    }

    public static void ValidateOutboundMetadata(
        IReadOnlyDictionary<string, string>? metadata,
        string parameterName) =>
        _ = CopyOutboundMetadata(metadata, parameterName);

    private static void ValidateKey(string key, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key, parameterName);

        if (key.Length > MaxMetadataKeyLength)
        {
            throw new ArgumentException(
                $"Stripe metadata keys cannot exceed {MaxMetadataKeyLength} characters.",
                parameterName);
        }

        if (key.Any(character => !IsAllowedKeyCharacter(character)))
        {
            throw new ArgumentException(
                "Stripe metadata keys may contain only letters, numbers, underscores, periods, colons, and hyphens.",
                parameterName);
        }

        if (SensitiveKeyFragments.Any(fragment => key.Contains(fragment, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException(
                $"Stripe metadata key '{key}' looks sensitive and cannot be sent to the provider.",
                parameterName);
        }
    }

    private static void ValidateValue(string value, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(value, parameterName);

        if (value.Length > MaxMetadataValueLength)
        {
            throw new ArgumentException(
                $"Stripe metadata values cannot exceed {MaxMetadataValueLength} characters.",
                parameterName);
        }
    }

    private static bool IsAllowedKeyCharacter(char character) =>
        char.IsAsciiLetterOrDigit(character)
        || character is '_' or '.' or ':' or '-';
}
