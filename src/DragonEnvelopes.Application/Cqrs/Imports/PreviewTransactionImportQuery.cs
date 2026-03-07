using DragonEnvelopes.Application.DTOs;

namespace DragonEnvelopes.Application.Cqrs.Imports;

public sealed record PreviewTransactionImportQuery(
    Guid FamilyId,
    Guid AccountId,
    string CsvContent,
    string? Delimiter,
    IReadOnlyDictionary<string, string>? HeaderMappings) : IQuery<ImportPreviewDetails>;
