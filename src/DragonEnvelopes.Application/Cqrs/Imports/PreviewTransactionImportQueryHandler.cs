using DragonEnvelopes.Application.DTOs;
using DragonEnvelopes.Application.Services;

namespace DragonEnvelopes.Application.Cqrs.Imports;

public sealed class PreviewTransactionImportQueryHandler(
    IImportService importService) : IQueryHandler<PreviewTransactionImportQuery, ImportPreviewDetails>
{
    public Task<ImportPreviewDetails> HandleAsync(
        PreviewTransactionImportQuery query,
        CancellationToken cancellationToken = default)
    {
        return importService.PreviewTransactionsAsync(
            query.FamilyId,
            query.AccountId,
            query.CsvContent,
            query.Delimiter,
            query.HeaderMappings,
            cancellationToken);
    }
}
