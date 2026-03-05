namespace DragonEnvelopes.Contracts.Imports;

public sealed record ImportCommitRequest(
    Guid FamilyId,
    Guid AccountId,
    string CsvContent,
    string? Delimiter,
    IReadOnlyDictionary<string, string>? HeaderMappings,
    IReadOnlyList<int>? AcceptedRowNumbers);
