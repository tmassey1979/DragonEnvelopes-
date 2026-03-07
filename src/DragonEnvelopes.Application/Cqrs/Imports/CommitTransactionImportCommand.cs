using DragonEnvelopes.Application.DTOs;

namespace DragonEnvelopes.Application.Cqrs.Imports;

public sealed record CommitTransactionImportCommand(
    Guid FamilyId,
    Guid AccountId,
    string CsvContent,
    string? Delimiter,
    IReadOnlyDictionary<string, string>? HeaderMappings,
    IReadOnlyList<int>? AcceptedRowNumbers) : ICommand<ImportCommitDetails>;
