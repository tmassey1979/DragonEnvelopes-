using System.Globalization;
using System.Text;
using DragonEnvelopes.Application.DTOs;
using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Domain;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Domain.ValueObjects;

namespace DragonEnvelopes.Application.Services;

public sealed class ImportService(
    ITransactionRepository transactionRepository,
    IImportDedupService importDedupService) : IImportService
{
    private static readonly IReadOnlyDictionary<string, string[]> HeaderAliases = new Dictionary<string, string[]>(
        StringComparer.OrdinalIgnoreCase)
    {
        ["occurredOn"] = ["date", "occurredat", "transactiondate", "posteddate", "occurredon"],
        ["amount"] = ["amount", "transactionamount", "amt"],
        ["merchant"] = ["merchant", "payee", "name"],
        ["description"] = ["description", "memo", "details"],
        ["category"] = ["category"]
    };

    public async Task<ImportPreviewDetails> PreviewTransactionsAsync(
        Guid familyId,
        Guid accountId,
        string csvContent,
        string? delimiter,
        IReadOnlyDictionary<string, string>? headerMappings,
        CancellationToken cancellationToken = default)
    {
        if (!await transactionRepository.AccountBelongsToFamilyAsync(accountId, familyId, cancellationToken))
        {
            throw new DomainValidationException("Account was not found for the provided family.");
        }

        var parsedRows = await ParseRowsAsync(
            accountId,
            csvContent,
            delimiter,
            headerMappings,
            cancellationToken);

        return new ImportPreviewDetails(
            parsedRows.Count,
            parsedRows.Count(static row => row.IsValid),
            parsedRows.Count(static row => row.IsDuplicate),
            parsedRows.Select(static row => new ImportPreviewRowDetails(
                    row.RowNumber,
                    row.OccurredOn,
                    row.Amount,
                    row.Merchant,
                    row.Description,
                    row.Category,
                    row.IsDuplicate,
                    row.Errors))
                .ToArray());
    }

    public async Task<ImportCommitDetails> CommitTransactionsAsync(
        Guid familyId,
        Guid accountId,
        string csvContent,
        string? delimiter,
        IReadOnlyDictionary<string, string>? headerMappings,
        IReadOnlyList<int>? acceptedRowNumbers,
        CancellationToken cancellationToken = default)
    {
        if (!await transactionRepository.AccountBelongsToFamilyAsync(accountId, familyId, cancellationToken))
        {
            throw new DomainValidationException("Account was not found for the provided family.");
        }

        var rows = await ParseRowsAsync(
            accountId,
            csvContent,
            delimiter,
            headerMappings,
            cancellationToken);
        var acceptedSet = acceptedRowNumbers is { Count: > 0 }
            ? acceptedRowNumbers.ToHashSet()
            : null;

        var eligibleRows = rows
            .Where(row => acceptedSet is null || acceptedSet.Contains(row.RowNumber))
            .ToArray();
        var insertable = eligibleRows
            .Where(static row => row.IsValid && !row.IsDuplicate)
            .Select(static row => new Transaction(
                Guid.NewGuid(),
                row.AccountId,
                Money.FromDecimal(row.Amount!.Value),
                row.Description!,
                row.Merchant!,
                new DateTimeOffset(row.OccurredOn!.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), TimeSpan.Zero),
                row.Category,
                envelopeId: null))
            .ToArray();

        await transactionRepository.AddTransactionsAsync(insertable, cancellationToken);

        return new ImportCommitDetails(
            rows.Count,
            rows.Count(static row => row.IsValid),
            rows.Count(static row => row.IsDuplicate),
            insertable.Length,
            eligibleRows.Length - insertable.Length);
    }

    private async Task<List<ParsedImportRow>> ParseRowsAsync(
        Guid accountId,
        string csvContent,
        string? delimiter,
        IReadOnlyDictionary<string, string>? headerMappings,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(csvContent))
        {
            throw new DomainValidationException("CSV content is required.");
        }

        var rows = ParseCsv(csvContent, delimiter);
        if (rows.Count < 2)
        {
            throw new DomainValidationException("CSV must include a header row and at least one data row.");
        }

        var header = rows[0];
        var headerIndexes = ResolveHeaderIndexes(header, headerMappings);
        var existingKeys = await BuildExistingKeysAsync(accountId, cancellationToken);
        var allSeenKeys = new HashSet<string>(existingKeys, StringComparer.Ordinal);
        var parsedRows = new List<ParsedImportRow>();
        for (var index = 1; index < rows.Count; index++)
        {
            var row = rows[index];
            if (row.All(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            var rowNumber = index + 1;
            var errors = new List<string>();
            var occurredOnText = GetValue(row, headerIndexes["occurredOn"]);
            var amountText = GetValue(row, headerIndexes["amount"]);
            var merchant = GetValue(row, headerIndexes["merchant"])?.Trim();
            var description = GetValue(row, headerIndexes["description"])?.Trim();
            var category = headerIndexes.TryGetValue("category", out var categoryIndex)
                ? GetValue(row, categoryIndex)?.Trim()
                : null;

            var occurredOn = ParseDate(occurredOnText, errors);
            var amount = ParseAmount(amountText, errors);

            if (string.IsNullOrWhiteSpace(merchant))
            {
                errors.Add("Merchant is required.");
            }

            if (string.IsNullOrWhiteSpace(description))
            {
                errors.Add("Description is required.");
            }

            var isDuplicate = false;
            if (errors.Count == 0)
            {
                var key = importDedupService.BuildKey(
                    accountId,
                    occurredOn!.Value,
                    amount!.Value,
                    merchant!,
                    description!);
                isDuplicate = !allSeenKeys.Add(key);
            }

            parsedRows.Add(new ParsedImportRow(
                rowNumber,
                accountId,
                occurredOn,
                amount,
                merchant,
                description,
                string.IsNullOrWhiteSpace(category) ? null : category,
                isDuplicate,
                errors));
        }

        return parsedRows;
    }

    private async Task<HashSet<string>> BuildExistingKeysAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var existing = await transactionRepository.ListTransactionsAsync(accountId, cancellationToken);
        return existing
            .Select(transaction => importDedupService.BuildKey(
                transaction.AccountId,
                DateOnly.FromDateTime(transaction.OccurredAt.UtcDateTime),
                transaction.Amount.Amount,
                transaction.Merchant,
                transaction.Description))
            .ToHashSet(StringComparer.Ordinal);
    }

    private static Dictionary<string, int> ResolveHeaderIndexes(
        IReadOnlyList<string> header,
        IReadOnlyDictionary<string, string>? headerMappings)
    {
        var indexByHeaderName = header
            .Select((column, index) => new { Column = column.Trim(), Index = index })
            .Where(static x => !string.IsNullOrWhiteSpace(x.Column))
            .GroupBy(static x => x.Column, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(static group => group.Key, static group => group.First().Index, StringComparer.OrdinalIgnoreCase);

        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var canonical in HeaderAliases.Keys)
        {
            if (headerMappings is not null && headerMappings.TryGetValue(canonical, out var mappedHeaderName))
            {
                if (indexByHeaderName.TryGetValue(mappedHeaderName.Trim(), out var mappedIndex))
                {
                    map[canonical] = mappedIndex;
                    continue;
                }
            }

            var alias = HeaderAliases[canonical].FirstOrDefault(indexByHeaderName.ContainsKey);
            if (!string.IsNullOrWhiteSpace(alias))
            {
                map[canonical] = indexByHeaderName[alias];
            }
        }

        var required = new[] { "occurredOn", "amount", "merchant", "description" };
        var missing = required.Where(canonical => !map.ContainsKey(canonical)).ToArray();
        if (missing.Length > 0)
        {
            throw new DomainValidationException(
                $"CSV is missing required column mappings: {string.Join(", ", missing)}.");
        }

        return map;
    }

    private static IReadOnlyList<IReadOnlyList<string>> ParseCsv(string csvContent, string? delimiter)
    {
        var parsedRows = new List<IReadOnlyList<string>>();
        var parsedRow = new List<string>();
        var fieldBuilder = new StringBuilder();
        var delimiterChar = string.IsNullOrEmpty(delimiter) ? ',' : delimiter[0];
        var inQuotes = false;

        for (var i = 0; i < csvContent.Length; i++)
        {
            var ch = csvContent[i];
            if (inQuotes)
            {
                if (ch == '"')
                {
                    if (i + 1 < csvContent.Length && csvContent[i + 1] == '"')
                    {
                        fieldBuilder.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    fieldBuilder.Append(ch);
                }

                continue;
            }

            if (ch == '"')
            {
                inQuotes = true;
                continue;
            }

            if (ch == delimiterChar)
            {
                parsedRow.Add(fieldBuilder.ToString());
                fieldBuilder.Clear();
                continue;
            }

            if (ch == '\r')
            {
                continue;
            }

            if (ch == '\n')
            {
                parsedRow.Add(fieldBuilder.ToString());
                fieldBuilder.Clear();
                parsedRows.Add(parsedRow.ToArray());
                parsedRow.Clear();
                continue;
            }

            fieldBuilder.Append(ch);
        }

        parsedRow.Add(fieldBuilder.ToString());
        if (parsedRow.Count > 1 || !string.IsNullOrWhiteSpace(parsedRow[0]))
        {
            parsedRows.Add(parsedRow.ToArray());
        }

        return parsedRows;
    }

    private static string? GetValue(IReadOnlyList<string> row, int index)
    {
        return index >= 0 && index < row.Count ? row[index] : null;
    }

    private static DateOnly? ParseDate(string? value, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add("Date is required.");
            return null;
        }

        var formats = new[] { "yyyy-MM-dd", "MM/dd/yyyy", "M/d/yyyy", "yyyy/MM/dd" };
        if (DateOnly.TryParseExact(
                value.Trim(),
                formats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var dateOnly))
        {
            return dateOnly;
        }

        if (DateOnly.TryParse(value.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out dateOnly))
        {
            return dateOnly;
        }

        if (DateTime.TryParse(value.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime))
        {
            return DateOnly.FromDateTime(dateTime);
        }

        errors.Add("Date is invalid.");
        return null;
    }

    private static decimal? ParseAmount(string? value, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add("Amount is required.");
            return null;
        }

        var style = NumberStyles.AllowLeadingSign |
                    NumberStyles.AllowDecimalPoint |
                    NumberStyles.AllowThousands |
                    NumberStyles.AllowCurrencySymbol;
        if (decimal.TryParse(value, style, CultureInfo.InvariantCulture, out var invariantAmount) ||
            decimal.TryParse(value, style, CultureInfo.CurrentCulture, out invariantAmount))
        {
            return decimal.Round(invariantAmount, 2, MidpointRounding.AwayFromZero);
        }

        errors.Add("Amount is invalid.");
        return null;
    }

    private sealed record ParsedImportRow(
        int RowNumber,
        Guid AccountId,
        DateOnly? OccurredOn,
        decimal? Amount,
        string? Merchant,
        string? Description,
        string? Category,
        bool IsDuplicate,
        IReadOnlyList<string> Errors)
    {
        public bool IsValid => Errors.Count == 0;
    }
}
