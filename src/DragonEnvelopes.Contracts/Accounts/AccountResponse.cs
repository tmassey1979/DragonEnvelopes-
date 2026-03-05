namespace DragonEnvelopes.Contracts.Accounts;

public sealed record AccountResponse(
    Guid Id,
    Guid FamilyId,
    string Name,
    string Type,
    decimal Balance);

