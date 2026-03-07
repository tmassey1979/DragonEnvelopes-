namespace DragonEnvelopes.Financial.Api.Services;

public enum ConsumerMessageDisposition
{
    Ack = 1,
    Retry = 2,
    DeadLetter = 3
}

public sealed record LedgerTransactionCreatedMessageProcessResult(
    ConsumerMessageDisposition Disposition,
    string IdempotencyKey,
    int AttemptCount,
    string? ErrorMessage);
