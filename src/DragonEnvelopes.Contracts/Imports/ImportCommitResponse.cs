namespace DragonEnvelopes.Contracts.Imports;

public sealed record ImportCommitResponse(
    int Parsed,
    int Valid,
    int Deduped,
    int Inserted,
    int Failed);
