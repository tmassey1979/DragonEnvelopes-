using DragonEnvelopes.Application.DTOs;

namespace DragonEnvelopes.Application.Services;

public interface IImportService
{
    Task<ImportPreviewDetails> PreviewTransactionsAsync(
        Guid familyId,
        Guid accountId,
        string csvContent,
        string? delimiter,
        IReadOnlyDictionary<string, string>? headerMappings,
        CancellationToken cancellationToken = default);

    Task<ImportCommitDetails> CommitTransactionsAsync(
        Guid familyId,
        Guid accountId,
        string csvContent,
        string? delimiter,
        IReadOnlyDictionary<string, string>? headerMappings,
        IReadOnlyList<int>? acceptedRowNumbers,
        CancellationToken cancellationToken = default);
}
