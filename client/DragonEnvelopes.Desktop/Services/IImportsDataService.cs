using DragonEnvelopes.Desktop.ViewModels;

namespace DragonEnvelopes.Desktop.Services;

public interface IImportsDataService
{
    Task<IReadOnlyList<AccountListItemViewModel>> GetAccountsAsync(CancellationToken cancellationToken = default);

    Task<ImportPreviewResultData> PreviewAsync(
        Guid accountId,
        string csvContent,
        string? delimiter,
        IReadOnlyDictionary<string, string>? headerMappings,
        CancellationToken cancellationToken = default);

    Task<ImportCommitResultData> CommitAsync(
        Guid accountId,
        string csvContent,
        string? delimiter,
        IReadOnlyDictionary<string, string>? headerMappings,
        IReadOnlyList<int>? acceptedRowNumbers,
        CancellationToken cancellationToken = default);
}
