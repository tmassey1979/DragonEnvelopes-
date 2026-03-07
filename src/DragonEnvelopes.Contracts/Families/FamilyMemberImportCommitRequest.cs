namespace DragonEnvelopes.Contracts.Families;

public sealed record FamilyMemberImportCommitRequest(
    string CsvContent,
    string? Delimiter,
    IReadOnlyDictionary<string, string>? HeaderMappings,
    IReadOnlyList<int>? AcceptedRowNumbers);
