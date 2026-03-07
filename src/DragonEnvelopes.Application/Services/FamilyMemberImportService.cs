using System.Text;
using DragonEnvelopes.Application.DTOs;
using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Domain;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Domain.ValueObjects;

namespace DragonEnvelopes.Application.Services;

public sealed class FamilyMemberImportService(IFamilyRepository familyRepository) : IFamilyMemberImportService
{
    private static readonly IReadOnlyDictionary<string, string[]> HeaderAliases = new Dictionary<string, string[]>(
        StringComparer.OrdinalIgnoreCase)
    {
        ["keycloakUserId"] = ["keycloakuserid", "keycloak_user_id", "userid", "user_id", "externaluserid", "external_user_id"],
        ["name"] = ["name", "fullname", "membername", "displayname"],
        ["email"] = ["email", "emailaddress", "mail", "email_address"],
        ["role"] = ["role", "memberrole", "member_role"]
    };

    public async Task<FamilyMemberImportPreviewDetails> PreviewAsync(
        Guid familyId,
        string csvContent,
        string? delimiter,
        IReadOnlyDictionary<string, string>? headerMappings,
        CancellationToken cancellationToken = default)
    {
        await EnsureFamilyExistsAsync(familyId, cancellationToken);
        var rows = await ParseRowsAsync(familyId, csvContent, delimiter, headerMappings, cancellationToken);

        return new FamilyMemberImportPreviewDetails(
            rows.Count,
            rows.Count(static row => row.IsValid),
            rows.Count(static row => row.IsDuplicate),
            rows.Select(static row => new FamilyMemberImportPreviewRowDetails(
                    row.RowNumber,
                    row.KeycloakUserId,
                    row.Name,
                    row.Email,
                    row.Role,
                    row.IsDuplicate,
                    row.Errors))
                .ToArray());
    }

    public async Task<FamilyMemberImportCommitDetails> CommitAsync(
        Guid familyId,
        string csvContent,
        string? delimiter,
        IReadOnlyDictionary<string, string>? headerMappings,
        IReadOnlyList<int>? acceptedRowNumbers,
        CancellationToken cancellationToken = default)
    {
        await EnsureFamilyExistsAsync(familyId, cancellationToken);
        var rows = await ParseRowsAsync(familyId, csvContent, delimiter, headerMappings, cancellationToken);

        var acceptedSet = acceptedRowNumbers is { Count: > 0 }
            ? acceptedRowNumbers.ToHashSet()
            : null;

        var eligibleRows = rows
            .Where(row => acceptedSet is null || acceptedSet.Contains(row.RowNumber))
            .ToArray();

        var insertableRows = eligibleRows
            .Where(static row => row.IsValid && !row.IsDuplicate)
            .ToArray();

        foreach (var row in insertableRows)
        {
            if (!Enum.TryParse<MemberRole>(row.Role, ignoreCase: true, out var parsedRole))
            {
                throw new DomainValidationException($"Row {row.RowNumber} role is invalid.");
            }

            var member = new FamilyMember(
                Guid.NewGuid(),
                familyId,
                row.KeycloakUserId!,
                row.Name!,
                EmailAddress.Parse(row.Email!),
                parsedRole);

            await familyRepository.AddMemberAsync(member, cancellationToken);
        }

        if (insertableRows.Length > 0)
        {
            await familyRepository.SaveChangesAsync(cancellationToken);
        }

        return new FamilyMemberImportCommitDetails(
            rows.Count,
            rows.Count(static row => row.IsValid),
            rows.Count(static row => row.IsDuplicate),
            insertableRows.Length,
            eligibleRows.Length - insertableRows.Length);
    }

    private async Task<List<ParsedImportRow>> ParseRowsAsync(
        Guid familyId,
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

        var headerIndexes = ResolveHeaderIndexes(rows[0], headerMappings);

        var existingMembers = await familyRepository.ListMembersAsync(familyId, cancellationToken);
        var seenKeycloakIds = existingMembers
            .Select(static member => NormalizeKey(member.KeycloakUserId))
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var seenEmails = existingMembers
            .Select(static member => NormalizeKey(member.Email.Value))
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

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
            var keycloakUserId = NormalizeOptional(GetValue(row, headerIndexes["keycloakUserId"]));
            var name = NormalizeOptional(GetValue(row, headerIndexes["name"]));
            var email = NormalizeOptional(GetValue(row, headerIndexes["email"]));
            var role = NormalizeOptional(GetValue(row, headerIndexes["role"]));

            if (string.IsNullOrWhiteSpace(keycloakUserId))
            {
                errors.Add("Keycloak user id is required.");
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                errors.Add("Name is required.");
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                errors.Add("Email is required.");
            }
            else
            {
                try
                {
                    email = EmailAddress.Parse(email).Value;
                }
                catch
                {
                    errors.Add("Email is invalid.");
                }
            }

            if (string.IsNullOrWhiteSpace(role))
            {
                errors.Add("Role is required.");
            }
            else if (!Enum.TryParse<MemberRole>(role, ignoreCase: true, out _))
            {
                errors.Add("Role is invalid.");
            }

            var isDuplicate = false;
            if (!string.IsNullOrWhiteSpace(keycloakUserId))
            {
                if (!seenKeycloakIds.Add(NormalizeKey(keycloakUserId)))
                {
                    errors.Add("Duplicate keycloak user id.");
                    isDuplicate = true;
                }
            }

            if (!string.IsNullOrWhiteSpace(email))
            {
                if (!seenEmails.Add(NormalizeKey(email)))
                {
                    errors.Add("Duplicate email.");
                    isDuplicate = true;
                }
            }

            parsedRows.Add(new ParsedImportRow(
                rowNumber,
                keycloakUserId,
                name,
                email,
                role,
                isDuplicate,
                errors));
        }

        return parsedRows;
    }

    private async Task EnsureFamilyExistsAsync(Guid familyId, CancellationToken cancellationToken)
    {
        var family = await familyRepository.GetFamilyByIdAsync(familyId, cancellationToken);
        if (family is null)
        {
            throw new DomainValidationException("Family was not found.");
        }
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

        var required = new[] { "keycloakUserId", "name", "email", "role" };
        var missing = required.Where(canonical => !map.ContainsKey(canonical)).ToArray();
        if (missing.Length > 0)
        {
            throw new DomainValidationException($"CSV is missing required column mappings: {string.Join(", ", missing)}.");
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

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static string NormalizeKey(string value)
    {
        return value.Trim().ToUpperInvariant();
    }

    private sealed record ParsedImportRow(
        int RowNumber,
        string? KeycloakUserId,
        string? Name,
        string? Email,
        string? Role,
        bool IsDuplicate,
        IReadOnlyList<string> Errors)
    {
        public bool IsValid => Errors.Count == 0;
    }
}
