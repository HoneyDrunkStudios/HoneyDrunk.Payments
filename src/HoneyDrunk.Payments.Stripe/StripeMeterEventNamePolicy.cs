namespace HoneyDrunk.Payments.Stripe;

internal static class StripeMeterEventNamePolicy
{
    internal const int MaxEventNameLength = 100;

    internal static string CreateFromKernelEvent(string eventType, string operationKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationKey);

        var eventName = Normalize($"{eventType}_{operationKey}", "billingEvent");
        ValidateEventName(eventName, "billingEvent");
        return eventName;
    }

    internal static void ValidateEventName(string eventName, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventName, parameterName);

        if (!IsValidEventName(eventName))
        {
            throw new ArgumentException(
                $"Stripe meter event names must be {MaxEventNameLength} characters or fewer and contain only lowercase ASCII letters, digits, and underscores.",
                parameterName);
        }
    }

    internal static bool IsValidEventName(string? eventName) =>
        !string.IsNullOrWhiteSpace(eventName)
        && eventName.Length <= MaxEventNameLength
        && eventName.Any(static character => char.IsAsciiLetterLower(character) || char.IsAsciiDigit(character))
        && eventName.All(static character =>
            char.IsAsciiLetterLower(character)
            || char.IsAsciiDigit(character)
            || character == '_');

    private static string Normalize(string value, string parameterName)
    {
        var normalized = new char[value.Length];
        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];
            if (char.IsAsciiLetterUpper(character))
            {
                normalized[index] = char.ToLowerInvariant(character);
                continue;
            }

            if (char.IsAsciiLetterLower(character)
                || char.IsAsciiDigit(character)
                || character == '_')
            {
                normalized[index] = character;
                continue;
            }

            if (character is '.' or '-')
            {
                normalized[index] = '_';
                continue;
            }

            throw new ArgumentException(
                "Kernel billing event type and operation key may contain only ASCII letters, digits, dots, hyphens, and underscores before Stripe meter-event normalization.",
                parameterName);
        }

        return new string(normalized);
    }
}
