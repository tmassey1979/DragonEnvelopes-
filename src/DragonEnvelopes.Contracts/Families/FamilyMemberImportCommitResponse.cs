namespace DragonEnvelopes.Contracts.Families;

public sealed record FamilyMemberImportCommitResponse(
    int Parsed,
    int Valid,
    int Deduped,
    int Inserted,
    int Failed);
