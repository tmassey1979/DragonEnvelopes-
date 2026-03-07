using DragonEnvelopes.Application.DTOs;

namespace DragonEnvelopes.Application.Services;

public interface IFamilyMemberImportService
{
    Task<FamilyMemberImportPreviewDetails> PreviewAsync(
        Guid familyId,
        string csvContent,
        string? delimiter,
        IReadOnlyDictionary<string, string>? headerMappings,
        CancellationToken cancellationToken = default);

    Task<FamilyMemberImportCommitDetails> CommitAsync(
        Guid familyId,
        string csvContent,
        string? delimiter,
        IReadOnlyDictionary<string, string>? headerMappings,
        IReadOnlyList<int>? acceptedRowNumbers,
        CancellationToken cancellationToken = default);
}
