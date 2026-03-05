namespace DragonEnvelopes.Contracts.Accounts;

public sealed record CreateAccountRequest(
    Guid FamilyId,
    string Name,
    string Type,
    decimal OpeningBalance);

