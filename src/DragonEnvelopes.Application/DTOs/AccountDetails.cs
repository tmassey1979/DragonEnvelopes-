namespace DragonEnvelopes.Application.DTOs;

public sealed record AccountDetails(
    Guid Id,
    Guid FamilyId,
    string Name,
    string Type,
    decimal Balance);
