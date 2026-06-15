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

    private static readonly string[] SensitiveValuePrefixes =
    [
        "sk_",
        "pk_",
        "rk_",
        "whsec_",
        "tok_",
        "card_",
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
            ValidateOutboundMetadataValue(value, parameterName);
            copy[key] = value;
        }

        return copy;
    }

    public static void ValidateOutboundMetadata(
        IReadOnlyDictionary<string, string>? metadata,
        string parameterName) =>
        _ = CopyOutboundMetadata(metadata, parameterName);

    public static void ValidateProviderReferenceValue(string value, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(value, parameterName);

        if (!IsSafeProviderMetadataValue(value))
        {
            throw new ArgumentException(
                $"Provider reference values cannot exceed {MaxMetadataValueLength} characters or contain sensitive-looking provider data.",
                parameterName);
        }
    }

    public static void ValidateOptionalProviderReferenceValue(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        ValidateProviderReferenceValue(value, parameterName);
    }

    public static void ValidateOutboundMetadataValue(string value, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(value, parameterName);

        if (!IsSafeOutboundMetadataValue(value))
        {
            throw new ArgumentException(
                $"Stripe metadata values cannot exceed {MaxMetadataValueLength} characters or contain sensitive-looking provider data.",
                parameterName);
        }
    }

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

    private static bool IsSafeMetadataValue(string? value) =>
        value is not null && value.Length <= MaxMetadataValueLength;

    private static bool IsSafeInboundMetadataValue(string? value) =>
        IsSafeOutboundMetadataValue(value);

    private static bool IsSafeOutboundMetadataValue(string? value) =>
        IsSafeProviderMetadataValue(value)
        && !LooksLikeSensitiveValue(value);

    private static bool IsSafeProviderMetadataValue(string? value) =>
        value is not null
        && IsSafeMetadataValue(value)
        && !value.Contains('@', StringComparison.Ordinal)
        && !SensitiveValuePrefixes.Any(prefix => value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

    private static bool LooksLikeSensitiveValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        var trimmed = value.Trim();
        return LooksLikePhoneNumber(trimmed)
            || LooksLikeCardNumber(trimmed)
            || LooksLikeSignature(trimmed)
            || LooksLikeOpaqueSecret(trimmed);
    }

    private static bool LooksLikePhoneNumber(string value)
    {
        var digitCount = value.Count(char.IsAsciiDigit);
        return digitCount >= 7
            && digitCount <= 15
            && value.All(static character =>
                char.IsAsciiDigit(character)
                || character is '+' or '-' or '.' or ' ' or '(' or ')');
    }

    private static bool LooksLikeCardNumber(string value)
    {
        var digitCount = value.Count(char.IsAsciiDigit);
        return digitCount >= 13
            && digitCount <= 19
            && value.All(static character =>
                char.IsAsciiDigit(character)
                || character is '-' or ' ');
    }

    private static bool LooksLikeSignature(string value) =>
        value.StartsWith("bearer ", StringComparison.OrdinalIgnoreCase)
        || value.StartsWith("basic ", StringComparison.OrdinalIgnoreCase)
        || value.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase)
        || value.StartsWith("v1=", StringComparison.OrdinalIgnoreCase)
        || value.StartsWith("sig_", StringComparison.OrdinalIgnoreCase)
        || value.Contains("-----BEGIN", StringComparison.OrdinalIgnoreCase);

    private static bool LooksLikeOpaqueSecret(string value) =>
        value.Length >= 32
        && !Guid.TryParseExact(value, "D", out _)
        && value.Any(char.IsAsciiLetter)
        && value.Any(char.IsAsciiDigit)
        && value.All(static character =>
            char.IsAsciiLetterOrDigit(character)
            || character is '_' or '-' or '+' or '/' or '=');

    private static bool IsAllowedKeyCharacter(char character) =>
        char.IsAsciiLetterOrDigit(character)
        || character is '_' or '.' or ':' or '-';
}
