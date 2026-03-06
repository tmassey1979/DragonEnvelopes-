namespace DragonEnvelopes.Desktop.ViewModels;

internal static class SensitiveValueMasker
{
    public static string MaskToken(string? value)
    {
        return Mask(value, preservePrefix: 6, preserveSuffix: 4);
    }

    public static string MaskIdentifier(string? value)
    {
        return Mask(value, preservePrefix: 4, preserveSuffix: 4);
    }

    private static string Mask(string? value, int preservePrefix, int preserveSuffix)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "-";
        }

        var normalized = value.Trim();
        if (normalized.Length <= preservePrefix + preserveSuffix + 2)
        {
            return new string('*', Math.Max(6, normalized.Length));
        }

        var middleLength = normalized.Length - preservePrefix - preserveSuffix;
        return $"{normalized[..preservePrefix]}{new string('*', middleLength)}{normalized[^preserveSuffix..]}";
    }
}
