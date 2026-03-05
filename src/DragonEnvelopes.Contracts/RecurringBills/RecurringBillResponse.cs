namespace DragonEnvelopes.Contracts.RecurringBills;

public sealed record RecurringBillResponse(
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
