using DragonEnvelopes.Application.DTOs;
using DragonEnvelopes.Application.Services;

namespace DragonEnvelopes.Application.Cqrs.Imports;

public sealed class CommitTransactionImportCommandHandler(
    IImportService importService) : ICommandHandler<CommitTransactionImportCommand, ImportCommitDetails>
{
    public Task<ImportCommitDetails> HandleAsync(
        CommitTransactionImportCommand command,
        CancellationToken cancellationToken = default)
    {
        return importService.CommitTransactionsAsync(
            command.FamilyId,
            command.AccountId,
            command.CsvContent,
            command.Delimiter,
            command.HeaderMappings,
            command.AcceptedRowNumbers,
            cancellationToken);
    }
}
