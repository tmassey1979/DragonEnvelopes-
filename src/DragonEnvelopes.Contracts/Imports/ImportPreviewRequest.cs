namespace DragonEnvelopes.Contracts.Imports;

public sealed record ImportPreviewRequest(
    Guid FamilyId,
    Guid AccountId,
    string CsvContent,
    string? Delimiter,
    IReadOnlyDictionary<string, string>? HeaderMappings);
