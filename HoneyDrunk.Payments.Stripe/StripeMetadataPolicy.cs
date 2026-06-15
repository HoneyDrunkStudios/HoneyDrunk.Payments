namespace HoneyDrunk.Payments.Stripe;

internal static class StripeMetadataPolicy
{
    internal const int MaxCustomMetadataEntries = 40;
    internal const int MaxMetadataKeyLength = 40;
    internal const int MaxMetadataValueLength = 500;

    private static readonly HashSet<string> AllowedInboundMetadataKeys = new(StringComparer.Ordinal)
    {
        "invoice_source",
        "payments_tenant_id",
        "project_id",
        "tier_name",
    };

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

    public static Dictionary<string, string> CopyInboundMetadata(IReadOnlyDictionary<string, string>? metadata)
    {
        var copy = new Dictionary<string, string>(StringComparer.Ordinal);
        if (metadata is null)
        {
            return copy;
        }

        foreach (var (key, value) in metadata)
        {
            if (copy.Count >= MaxCustomMetadataEntries)
            {
                break;
            }

            if (IsAllowedInboundMetadata(key, value))
            {
                copy[key] = value;
            }
        }

        return copy;
    }

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

    private static bool IsSafeMetadataKey(string? key) =>
        !string.IsNullOrWhiteSpace(key)
        && key.Length <= MaxMetadataKeyLength
        && key.All(IsAllowedKeyCharacter)
        && !SensitiveKeyFragments.Any(fragment => key.Contains(fragment, StringComparison.OrdinalIgnoreCase));

    private static bool IsAllowedInboundMetadata(string? key, string? value) =>
        key is not null
        && IsSafeMetadataKey(key)
        && AllowedInboundMetadataKeys.Contains(key)
        && IsSafeInboundMetadataValue(value);

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

    private static bool IsSafeMetadataValue(string? value) =>
        value is not null && value.Length <= MaxMetadataValueLength;

    private static bool IsSafeInboundMetadataValue(string? value) =>
        value is not null
        && IsSafeMetadataValue(value)
        && !value.Contains('@', StringComparison.Ordinal)
        && !SensitiveKeyFragments.Any(fragment => value.Contains(fragment, StringComparison.OrdinalIgnoreCase))
        && !value.StartsWith("sk_", StringComparison.OrdinalIgnoreCase)
        && !value.StartsWith("pk_", StringComparison.OrdinalIgnoreCase)
        && !value.StartsWith("rk_", StringComparison.OrdinalIgnoreCase)
        && !value.StartsWith("whsec_", StringComparison.OrdinalIgnoreCase)
        && !value.StartsWith("tok_", StringComparison.OrdinalIgnoreCase)
        && !value.StartsWith("card_", StringComparison.OrdinalIgnoreCase);

    private static bool IsAllowedKeyCharacter(char character) =>
        char.IsAsciiLetterOrDigit(character)
        || character is '_' or '.' or ':' or '-';
}
