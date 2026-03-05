namespace DragonEnvelopes.Application.DTOs;

public sealed record RecurringBillDetails(
    Guid Id,
    Guid FamilyId,
    string Name,
    string Merchant,
    decimal Amount,
    string Frequency,
    int DayOfMonth,
    DateOnly StartDate,
    DateOnly? EndDate,
    bool IsActive);

public sealed record RecurringBillProjectionItemDetails(
    Guid RecurringBillId,
    string Name,
    string Merchant,
    decimal Amount,
    DateOnly DueDate);
