namespace DragonEnvelopes.Contracts.Families;

public sealed record FamilyMemberImportPreviewRequest(
    string CsvContent,
    string? Delimiter,
    IReadOnlyDictionary<string, string>? HeaderMappings);
