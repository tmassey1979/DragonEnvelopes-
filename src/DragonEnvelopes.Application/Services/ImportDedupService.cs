using System.Text;

namespace DragonEnvelopes.Application.Services;

public sealed class ImportDedupService : IImportDedupService
{
    public string BuildKey(
        Guid accountId,
        DateOnly occurredOn,
        decimal amount,
        string merchant,
        string description)
    {
        var normalizedMerchant = NormalizeText(merchant);
        var normalizedDescription = NormalizeText(description);
        return $"{accountId:N}|{occurredOn:yyyy-MM-dd}|{amount:0.00}|{normalizedMerchant}|{normalizedDescription}";
    }

    private static string NormalizeText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var source = value.Trim().ToLowerInvariant();
        var sb = new StringBuilder(source.Length);
        var previousWasWhitespace = false;
        foreach (var ch in source)
        {
            if (char.IsWhiteSpace(ch))
            {
                if (!previousWasWhitespace)
                {
                    sb.Append(' ');
                    previousWasWhitespace = true;
                }

                continue;
            }

            sb.Append(ch);
            previousWasWhitespace = false;
        }

        return sb.ToString();
    }
}
